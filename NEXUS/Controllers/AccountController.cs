using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Models;

namespace NEXUS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==================== User Profile Actions ====================

        // GET: /Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // جلب التاسكات الخاصة بالمستخدم
            var userTasks = await _context.TaskItems
                .Where(t => t.ProjectId == user.ProjectId)
                .ToListAsync();

            // جلب المشروع المرتبط به إن وجد مع الأقسام والتيمات
            var userProject = user.ProjectId != null
                ? await _context.Projects.Include(p => p.Department).Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == user.ProjectId)
                : null;

            // جلب التيمات الحقيقية التي ينتمي إليها المستخدم
            var userTeams = await _context.Teams
                .Where(t => t.Users.Any(u => u.Id == user.Id))
                .ToListAsync();

            // القسم الحقيقي التابع للمشروع أو المستخدم
            var userDepartment = userProject?.Department;

            int totalUserTasks = userTasks.Count;
            int completedUserTasks = userTasks.Count(t => t.Status == Enums.TaskStatus.Completed);
            double userTaskRate = totalUserTasks > 0 ? Math.Round(((double)completedUserTasks / totalUserTasks) * 100, 1) : 0;

            ViewBag.UserProject = userProject;
            ViewBag.UserDepartment = userDepartment;
            ViewBag.UserTeams = userTeams;
            ViewBag.UserTasks = userTasks;
            ViewBag.UserTaskRate = userTaskRate;
            ViewBag.CompletedUserTasks = completedUserTasks;
            ViewBag.TotalUserTasks = totalUserTasks;

            return View(user);
        }

        // POST: /Account/Profile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model, string currentPassword, string newPassword, string aboutText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return RedirectToAction(nameof(Profile));
                }
            }

            if (updateResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }

            return RedirectToAction(nameof(Profile));
        }
    }

    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}