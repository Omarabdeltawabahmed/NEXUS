using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.ViewModels;
using System.Diagnostics;
using NEXUS.Models;
using NEXUS.Enums;
using TaskStatus = NEXUS.Enums.TaskStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace NEXUS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            int totalTasks = await _context.TaskItems.CountAsync();
            int completedTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskStatus.Completed);
            int todoTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskStatus.Pending);
            int inProgressTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskStatus.InProgress);

            int activeProjects = await _context.Projects.CountAsync(p => p.Status == NEXUS.Enums.ProjectStatus.Active);
            int totalDepartments = await _context.Departments.CountAsync();

            // حساب مؤشر الإنتاجية بناءً على نسبة المهام المكتملة
            double productivity = totalTasks > 0 ? Math.Round(((double)completedTasks / totalTasks) * 100, 1) : 0;

            var recentTasks = await _context.TaskItems
              .Include(t => t.Project)                      
              .OrderByDescending(t => t.Id)
              .Take(5)
              .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                ActiveProjects = activeProjects,
                TotalDepartments = totalDepartments,
                ProductivityIndex = productivity,
                TasksToDo = todoTasks,
                TasksInProgress = inProgressTasks,
                TasksCompletedCount = completedTasks,
                RecentTasks = recentTasks
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string? query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;
            bool isQuickBrowse = string.IsNullOrWhiteSpace(query);
            var term = query?.Trim();

            var deptQuery = _context.Departments.AsQueryable();
            if (!isQuickBrowse && !string.IsNullOrEmpty(term))
            {
                deptQuery = deptQuery.Where(d => EF.Functions.Like(d.Name, $"%{term}%") || (d.Description != null && EF.Functions.Like(d.Description, $"%{term}%")));
            }

            var departments = await deptQuery
                .Take(4)
                .Select(d => new
                {
                    id = d.Id,
                    title = d.Name,
                    subtitle = !string.IsNullOrEmpty(d.Description) ? d.Description : "Department",
                    badge = "Department",
                    badgeColor = "cyan",
                    icon = "fa-solid fa-building",
                    url = Url.Action("Details", "Departments", new { id = d.Id })
                })
                .ToListAsync();

            var projQuery = _context.Projects.Include(p => p.Department).AsQueryable();
            if (!isQuickBrowse && !string.IsNullOrEmpty(term))
            {
                projQuery = projQuery.Where(p => EF.Functions.Like(p.Name, $"%{term}%") || (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")));
            }

            var projects = await projQuery
                .OrderByDescending(p => p.Id)
                .Take(4)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Name,
                    subtitle = p.Department != null ? p.Department.Name : "Project",
                    badge = p.Status.ToString(),
                    badgeColor = "emerald",
                    icon = "fa-solid fa-layer-group",
                    url = Url.Action("Details", "Projects", new { id = p.Id })
                })
                .ToListAsync();

            var taskQuery = _context.TaskItems.Include(t => t.Project).AsQueryable();
            if (!isQuickBrowse && !string.IsNullOrEmpty(term))
            {
                taskQuery = taskQuery.Where(t => EF.Functions.Like(t.Title, $"%{term}%") || (t.Description != null && EF.Functions.Like(t.Description, $"%{term}%")));
            }

            var tasks = await taskQuery
                .OrderByDescending(t => t.Id)
                .Take(4)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    subtitle = t.Project != null ? t.Project.Name : "Task",
                    badge = t.Status.ToString(),
                    badgeColor = "violet",
                    priority = t.Priority.ToString(),
                    icon = "fa-solid fa-square-check",
                    url = (currentUserId != null && t.AssignedToUserId == currentUserId && t.Status != TaskStatus.Completed)
                        ? Url.Action("StartTask", "Tasks", new { id = t.Id })
                        : Url.Action("Details", "Tasks", new { id = t.Id })
                })
                .ToListAsync();

            var teamQuery = _context.Teams.AsQueryable();
            if (!isQuickBrowse && !string.IsNullOrEmpty(term))
            {
                teamQuery = teamQuery.Where(t => EF.Functions.Like(t.Name, $"%{term}%") || (t.Description != null && EF.Functions.Like(t.Description, $"%{term}%")));
            }

            var teams = await teamQuery
                .Take(4)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Name,
                    subtitle = !string.IsNullOrEmpty(t.Description) ? t.Description : "Team",
                    badge = "Team",
                    badgeColor = "amber",
                    icon = "fa-solid fa-users",
                    url = Url.Action("Details", "Teams", new { id = t.Id })
                })
                .ToListAsync();

            return Json(new { departments, projects, tasks, teams, isQuickBrowse });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { count = 0, tasks = new object[] { } });
            }

            var tasks = await _context.TaskItems
                .Include(t => t.Project)
                .Where(t => t.AssignedToUserId == currentUser.Id && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled)
                .OrderBy(t => t.Status == TaskStatus.Pending ? 0 : 1)
                .ThenByDescending(t => t.DueDate)
                .Take(10)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    project = t.Project != null ? t.Project.Name : "General",
                    priority = t.Priority.ToString(),
                    status = t.Status.ToString(),
                    isPending = t.Status == TaskStatus.Pending,
                    dueDate = t.DueDate.ToString("MMM dd"),
                    url = Url.Action("StartTask", "Tasks", new { id = t.Id })
                })
                .ToListAsync();

            var totalActiveCount = tasks.Count;

            return Json(new { count = totalActiveCount, tasks });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}