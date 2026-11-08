using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Models;
using TaskStatus = NEXUS.Enums.TaskStatus;

namespace NEXUS.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<List<ApplicationUser>> GetNonAdminUsersAsync()
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = new List<string>();
            if (adminRole != null)
            {
                adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();
            }

            return await _context.Users
                .Where(u => !adminUserIds.Contains(u.Id))
                .ToListAsync();
        }

        // GET: Tasks
        public async Task<IActionResult> Index(bool viewAll = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .AsQueryable();

            if (!isAdmin && !viewAll)
            {
                if (currentUser != null)
                {
                    query = query.Where(t => t.AssignedToUserId == currentUser.Id);
                }
            }

            ViewBag.ViewAll = viewAll;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = currentUser?.Id;

            return View(await query.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskItem == null) return NotFound();

            return View(taskItem);
        }

        // GET: Tasks/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            ViewData["Type"] = new SelectList(Enum.GetValues(typeof(NEXUS.Enums.TaskType)));
            var validUsers = await GetNonAdminUsersAsync();
            ViewData["AssignedToUserId"] = new SelectList(validUsers, "Id", "FullName");
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,DueDate,Priority,Status,ProjectId,Type,AssignedToUserId,MeetingLink,ApiLink")] TaskItem taskItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taskItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["Type"] = new SelectList(Enum.GetValues(typeof(NEXUS.Enums.TaskType)), taskItem.Type);
            var validUsers = await GetNonAdminUsersAsync();
            ViewData["AssignedToUserId"] = new SelectList(validUsers, "Id", "FullName", taskItem.AssignedToUserId);
            return View(taskItem);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetUsersByProject(int projectId)
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = adminRole != null ? await _context.UserRoles.Where(ur => ur.RoleId == adminRole.Id).Select(ur => ur.UserId).ToListAsync() : new List<string>();

            var users = await _context.Users
                .Where(u => u.ProjectId == projectId && !adminUserIds.Contains(u.Id))
                .Select(u => new { id = u.Id, name = u.FullName ?? u.Email })
                .ToListAsync();

            return Json(users);
        }

        // GET: Tasks/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null) return NotFound();

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["Type"] = new SelectList(Enum.GetValues(typeof(NEXUS.Enums.TaskType)), taskItem.Type);
            var validUsers = await GetNonAdminUsersAsync();
            ViewData["AssignedToUserId"] = new SelectList(validUsers, "Id", "FullName", taskItem.AssignedToUserId);

            return View(taskItem);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DueDate,Priority,Status,ProjectId,Type,AssignedToUserId,MeetingLink,ApiLink")] TaskItem taskItem)
        {
            if (id != taskItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskItemExists(taskItem.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", taskItem.ProjectId);
            ViewData["Type"] = new SelectList(Enum.GetValues(typeof(NEXUS.Enums.TaskType)), taskItem.Type);
            var validUsers = await GetNonAdminUsersAsync();
            ViewData["AssignedToUserId"] = new SelectList(validUsers, "Id", "FullName", taskItem.AssignedToUserId);
            return View(taskItem);
        }

        // GET: Tasks/StartTask/5
        public async Task<IActionResult> StartTask(int? id)
        {
            if (id == null) return NotFound();

            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskItem == null) return NotFound();

            if (taskItem.Status == TaskStatus.Pending)
            {
                taskItem.Status = TaskStatus.InProgress;
                _context.Update(taskItem);
                await _context.SaveChangesAsync();
            }

            return View(taskItem);
        }

        // POST: Tasks/SubmitTask/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTask(int id, string? submissionContent, IFormFile? uploadedFile)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null) return NotFound();

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                taskItem.SubmissionContent = "/uploads/" + uniqueFileName;
            }
            else if (!string.IsNullOrEmpty(submissionContent))
            {
                taskItem.SubmissionContent = submissionContent;
            }

            taskItem.Status = TaskStatus.Completed;
            taskItem.SubmittedAt = DateTime.Now;

            _context.Update(taskItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/SubmittedTasks
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SubmittedTasks()
        {
            var submittedTasks = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Where(t => t.Status == TaskStatus.Completed)
                .OrderByDescending(t => t.DueDate)
                .ToListAsync();

            return View(submittedTasks);
        }

        // GET: Tasks/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskItem == null) return NotFound();

            return View(taskItem);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem != null)
            {
                _context.TaskItems.Remove(taskItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskItemExists(int id)
        {
            return _context.TaskItems.Any(e => e.Id == id);
        }
    }
}