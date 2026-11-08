using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Models;

namespace NEXUS.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // مساعدة لجلب المستخدمين العاديين فقط (استبعاد الـ Admin)
        private async Task<List<ApplicationUser>> GetNonAdminUsersAsync()
        {
            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            var adminUserIds = await _context.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToListAsync();
            return await _context.Users.Where(u => !adminUserIds.Contains(u.Id)).ToListAsync();
        }

        // 1. GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.Team)
                .ToListAsync();

            foreach (var project in projects)
            {
                var projectTasks = await _context.TaskItems
                    .Where(t => t.ProjectId == project.Id)
                    .ToListAsync();

                bool hasTasks = projectTasks.Any();
                bool allCompleted = hasTasks && projectTasks.All(t => t.Status == NEXUS.Enums.TaskStatus.Completed);

                var targetStatus = allCompleted ? NEXUS.Enums.ProjectStatus.Completed : NEXUS.Enums.ProjectStatus.Active;

                if (project.Status != targetStatus)
                {
                    project.Status = targetStatus;
                    _context.Update(project);
                }
            }
            await _context.SaveChangesAsync();

            projects = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.Team)
                .ToListAsync();

            return View(projects);
        }

        // 2. GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.Team)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            project.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == id)
                .ToListAsync();

            project.Users = await _context.Users
                .Where(u => u.ProjectId == id)
                .ToListAsync();

            return View(project);
        }

        // 3. GET: Projects/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.TeamsList = await _context.Teams.ToListAsync();
            return View();
        }

        // 4. POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Project project, List<string> selectedUserIds)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();

                if (selectedUserIds != null && selectedUserIds.Any())
                {
                    foreach (var userId in selectedUserIds)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.ProjectId = project.Id;
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", project.DepartmentId);
            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.TeamsList = await _context.Teams.ToListAsync();

            return View(project);
        }

        // 5. GET: Projects/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", project.DepartmentId);
            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.TeamsList = await _context.Teams.ToListAsync();
            ViewBag.AssignedUserIds = await _context.Users
                .Where(u => u.ProjectId == id)
                .Select(u => u.Id)
                .ToListAsync();

            return View(project);
        }

        // 6. POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Project project, List<string> selectedUserIds)
        {
            if (id != project.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var projectToUpdate = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
                    if (projectToUpdate == null) return NotFound();

                    projectToUpdate.Name = project.Name;
                    projectToUpdate.Description = project.Description;
                    projectToUpdate.DepartmentId = project.DepartmentId;
                    projectToUpdate.TeamId = project.TeamId;

                    var currentUsers = await _context.Users.Where(u => u.ProjectId == id).ToListAsync();
                    selectedUserIds ??= new List<string>();

                    foreach (var user in currentUsers)
                    {
                        if (!selectedUserIds.Contains(user.Id))
                        {
                            user.ProjectId = null;
                        }
                    }

                    foreach (var userId in selectedUserIds)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.ProjectId = id;
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Projects.Any(e => e.Id == project.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Details), new { id = id });
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", project.DepartmentId);
            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.TeamsList = await _context.Teams.ToListAsync();
            ViewBag.AssignedUserIds = selectedUserIds;

            return View(project);
        }

        // 7. GET: Projects/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Department)
                .Include(p => p.Team)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // 8. POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                var projectUsers = await _context.Users.Where(u => u.ProjectId == id).ToListAsync();
                foreach (var user in projectUsers)
                {
                    user.ProjectId = null;
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}