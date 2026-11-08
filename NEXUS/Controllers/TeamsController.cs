using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Models;

namespace NEXUS.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly AppDbContext _context;

        public TeamsController(AppDbContext context)
        {
            _context = context;
        }

        // مساعدة لجلب المستخدمين العاديين فقط (استبعاد الـ Admin)
        private async Task<List<ApplicationUser>> GetNonAdminUsersAsync()
        {
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var adminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            return await _context.Users
                .Where(u => !adminUserIds.Contains(u.Id))
                .ToListAsync();
        }

        // 1. GET: Teams
        public async Task<IActionResult> Index()
        {
            var teams = await _context.Teams
                .Include(t => t.Users)
                .Include(t => t.Projects)
                .ToListAsync();
            return View(teams);
        }

        // 2. GET: Teams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Users)
                .Include(t => t.Projects)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            return View(team);
        }

        // 3. GET: Teams/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            return View();
        }

        // 4. POST: Teams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team, List<string> selectedUserIds)
        {
            if (ModelState.IsValid)
            {
                selectedUserIds ??= new List<string>();
                foreach (var userId in selectedUserIds)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        team.Users.Add(user);
                    }
                }

                _context.Add(team);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            return View(team);
        }

        // 5. GET: Teams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.AssignedUserIds = team.Users.Select(u => u.Id).ToList();

            return View(team);
        }

        // 6. POST: Teams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Team team, List<string> selectedUserIds)
        {
            if (id != team.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var teamToUpdate = await _context.Teams
                        .Include(t => t.Users)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (teamToUpdate == null) return NotFound();

                    teamToUpdate.Name = team.Name;
                    teamToUpdate.Description = team.Description;

                    // تحديث الأعضاء
                    teamToUpdate.Users.Clear();
                    selectedUserIds ??= new List<string>();
                    foreach (var userId in selectedUserIds)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            teamToUpdate.Users.Add(user);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Teams.Any(e => e.Id == team.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllUsers = await GetNonAdminUsersAsync();
            ViewBag.AssignedUserIds = selectedUserIds;
            return View(team);
        }

        // 7. GET: Teams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Users)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            return View(team);
        }

        // 8. POST: Teams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Projects)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team != null)
            {
                // فك ارتباط المشاريع بهذا الفريق أولاً
                foreach (var project in team.Projects)
                {
                    project.TeamId = null;
                }

                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}