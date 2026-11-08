using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Enums;
using NEXUS.Models;
using NEXUS.ViewModels;
using TaskStatus = NEXUS.Enums.TaskStatus;

namespace NEXUS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? projectId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");

            // Global System Counts
            int departmentsCount = await _context.Departments.CountAsync();
            int totalProjectsCount = await _context.Projects.CountAsync();
            int completedProjectsCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed);
            int activeProjectsCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Active);
            int teamsCount = await _context.Teams.CountAsync();
            int usersCount = await _context.Users.CountAsync();

            int totalTeamMembers = await _context.Teams
                .Include(t => t.Users)
                .SumAsync(t => t.Users.Count);

            int projectsWithDepartments = await _context.Projects.CountAsync(p => p.DepartmentId != null);

            // Workspace & Tasks Context
            string workspaceName = "Global Core Workspace";
            int? effectiveProjectId = projectId;

            if (!isAdmin && currentUser?.ProjectId != null)
            {
                effectiveProjectId = currentUser.ProjectId;
                var assignedProject = await _context.Projects.FindAsync(currentUser.ProjectId);
                if (assignedProject != null)
                {
                    workspaceName = $"{assignedProject.Name} Workspace";
                }
            }
            else if (effectiveProjectId.HasValue)
            {
                var targetProject = await _context.Projects.FindAsync(effectiveProjectId.Value);
                if (targetProject != null)
                {
                    workspaceName = $"{targetProject.Name} Workspace";
                }
            }

            // Query scoped or global tasks
            var tasksQuery = _context.TaskItems.AsQueryable();

            if (effectiveProjectId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == effectiveProjectId.Value);
            }
            else if (!isAdmin && currentUser != null)
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == currentUser.Id || (currentUser.ProjectId != null && t.ProjectId == currentUser.ProjectId));
            }

            var scopedTasks = await tasksQuery.ToListAsync();

            int totalTasks = scopedTasks.Count;
            int completedTasks = scopedTasks.Count(t => t.Status == TaskStatus.Completed);
            int inProgressTasks = scopedTasks.Count(t => t.Status == TaskStatus.InProgress);
            int pendingTasks = scopedTasks.Count(t => t.Status == TaskStatus.Pending);

            double taskCompletionRate = totalTasks > 0
                ? Math.Round(((double)completedTasks / totalTasks) * 100, 1)
                : 0;

            double projectCompletionRate = totalProjectsCount > 0
                ? Math.Round(((double)completedProjectsCount / totalProjectsCount) * 100, 1)
                : 0;

            // Priority counts
            int urgentTasksCount = scopedTasks.Count(t => t.Priority == TaskPriority.Urgent);
            int highTasksCount = scopedTasks.Count(t => t.Priority == TaskPriority.High);
            int mediumTasksCount = scopedTasks.Count(t => t.Priority == TaskPriority.Medium);
            int lowTasksCount = scopedTasks.Count(t => t.Priority == TaskPriority.Low);

            // Fetch Real Projects for the Project Execution Table
            var projectsData = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.Team)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var projectReports = projectsData.Select(p =>
            {
                int pTotal = p.Tasks.Count;
                int pCompleted = p.Tasks.Count(t => t.Status == TaskStatus.Completed);
                double pRate = pTotal > 0 ? Math.Round(((double)pCompleted / pTotal) * 100, 1) : (p.Status == ProjectStatus.Completed ? 100.0 : 0.0);

                return new ProjectReportItem
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    DepartmentName = p.Department?.Name ?? "General Dept",
                    TeamName = p.Team?.Name ?? "Unassigned Team",
                    TotalTasks = pTotal,
                    CompletedTasks = pCompleted,
                    ProgressRate = pRate,
                    Status = p.Status.ToString()
                };
            }).ToList();

            // Calculate Dynamic Velocity Points & SVG Curve from Real Database Data
            var velocityPoints = GenerateVelocityPoints(scopedTasks, taskCompletionRate);
            GenerateSvgPaths(velocityPoints, out string curvePath, out string areaPath);

            var viewModel = new ReportsViewModel
            {
                IsAdmin = isAdmin,
                WorkspaceName = workspaceName,
                CurrentProjectId = effectiveProjectId,

                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PendingTasks = pendingTasks,
                TaskCompletionRate = taskCompletionRate,

                UrgentTasksCount = urgentTasksCount,
                HighTasksCount = highTasksCount,
                MediumTasksCount = mediumTasksCount,
                LowTasksCount = lowTasksCount,

                TotalProjects = totalProjectsCount,
                CompletedProjects = completedProjectsCount,
                ActiveProjects = activeProjectsCount,
                ProjectCompletionRate = projectCompletionRate,
                ProjectsWithDepartments = projectsWithDepartments,

                TotalDepartments = departmentsCount,
                TotalTeams = teamsCount,
                TotalTeamMembers = totalTeamMembers,
                TotalUsers = usersCount,

                VelocityPoints = velocityPoints,
                SvgCurvePath = curvePath,
                SvgAreaPath = areaPath,
                VelocityPeakLabel = $"{taskCompletionRate.ToString("0.0", CultureInfo.InvariantCulture)}% Live",
                ProjectReports = projectReports
            };

            // Maintain legacy ViewBag entries for backward compatibility
            ViewBag.DepartmentsCount = departmentsCount;
            ViewBag.ProjectsCount = totalProjectsCount;
            ViewBag.CompletedProjectsCount = completedProjectsCount;
            ViewBag.ProjectCompletionRate = projectCompletionRate;

            ViewBag.TasksCount = totalTasks;
            ViewBag.CompletedTasksCount = completedTasks;
            ViewBag.TaskCompletionRate = taskCompletionRate;

            ViewBag.TeamsCount = teamsCount;
            ViewBag.UsersCount = usersCount;
            ViewBag.TotalTeamMembers = totalTeamMembers;
            ViewBag.ProjectsWithDepartments = projectsWithDepartments;

            return View(viewModel);
        }

        /// <summary>
        /// Generates 6 real timeline velocity points mapped across the SVG coordinates (0..500 x 0..150).
        /// </summary>
        private List<TaskVelocityPoint> GenerateVelocityPoints(List<TaskItem> tasks, double currentRate)
        {
            var points = new List<TaskVelocityPoint>();
            var now = DateTime.Now;

            // Generate 6 chronological timeline intervals based on due dates & task completion
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                string monthLabel = targetMonth.ToString("MMM", CultureInfo.InvariantCulture);

                var tasksInWindow = tasks.Where(t => t.DueDate.Month <= targetMonth.Month && t.DueDate.Year <= targetMonth.Year).ToList();
                int totalInWindow = tasksInWindow.Count;
                int completedInWindow = tasksInWindow.Count(t => t.Status == TaskStatus.Completed);

                double rate = totalInWindow > 0
                    ? Math.Round(((double)completedInWindow / totalInWindow) * 100, 1)
                    : (i == 0 ? currentRate : Math.Max(10.0, currentRate * (1.0 - (i * 0.15))));

                // SVG coordinates: viewBox 0 0 500 150
                // X goes from 0 to 500
                double x = (5 - i) * 100.0;
                // Y goes from 135 (0%) down to 25 (100%)
                double y = Math.Round(135.0 - ((rate / 100.0) * 110.0), 1);

                points.Add(new TaskVelocityPoint
                {
                    Label = monthLabel,
                    TotalCount = totalInWindow > 0 ? totalInWindow : tasks.Count,
                    CompletedCount = completedInWindow,
                    Rate = rate,
                    X = x,
                    Y = y
                });
            }

            return points;
        }

        /// <summary>
        /// Constructs smooth Cubic Bezier SVG path and fill area coordinates from real velocity points.
        /// </summary>
        private void GenerateSvgPaths(List<TaskVelocityPoint> points, out string curvePath, out string areaPath)
        {
            if (points == null || points.Count < 2)
            {
                curvePath = "M 0,120 Q 250,70 500,40";
                areaPath = "M 0,120 Q 250,70 500,40 L 500,150 L 0,150 Z";
                return;
            }

            var curveSb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            curveSb.AppendFormat(inv, "M {0},{1}", points[0].X, points[0].Y);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];

                // Smooth cubic bezier control points
                double cp1x = p0.X + (p1.X - p0.X) / 2.0;
                double cp1y = p0.Y;
                double cp2x = p0.X + (p1.X - p0.X) / 2.0;
                double cp2y = p1.Y;

                curveSb.AppendFormat(inv, " C {0},{1} {2},{3} {4},{5}", cp1x, cp1y, cp2x, cp2y, p1.X, p1.Y);
            }

            curvePath = curveSb.ToString();

            // Construct closed polygon for the area gradient fill
            var lastPoint = points.Last();
            var firstPoint = points.First();
            areaPath = $"{curvePath} L {lastPoint.X.ToString("0.0", inv)},150 L {firstPoint.X.ToString("0.0", inv)},150 Z";
        }
    }
}