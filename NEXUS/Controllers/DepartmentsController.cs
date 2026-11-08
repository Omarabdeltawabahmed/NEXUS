using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NEXUS.Data;
using NEXUS.Models;

namespace NEXUS.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments.Include(d => d.Projects).ToListAsync();
            return View(departments);
        }

        // 2. GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.Include(d => d.Projects).FirstOrDefaultAsync(m => m.Id == id);
            if (department == null) return NotFound();

            return View(department);
        }

        // 3. GET: Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // 5. GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return NotFound();

            // جلب جميع المشاريع المتاحة في النظام لتعرض في شكل كروت تفاعلية
            ViewBag.AllProjects = await _context.Projects.ToListAsync();

            // تحديد أيدي المشاريع المرتبطة بهذا القسم مسبقاً
            ViewBag.AssignedProjectIds = department.Projects.Select(p => p.Id).ToList();

            return View(department);
        }

        // 6. POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department, List<int> selectedProjectIds)
        {
            if (id != department.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);

                    // تحديث ربط المشاريع بالقسم بناءً على المحدد في الـ Checkbox
                    var allProjects = await _context.Projects.ToListAsync();
                    selectedProjectIds ??= new List<int>();

                    foreach (var project in allProjects)
                    {
                        if (selectedProjectIds.Contains(project.Id))
                        {
                            project.DepartmentId = id; // ربط المشروع بهذا القسم
                        }
                        else if (project.DepartmentId == id)
                        {
                            project.DepartmentId = null; // إزالة الربط إذا تم إلغاء التحديد
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Departments.Any(e => e.Id == department.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Details), new { id = id });
            }

            ViewBag.AllProjects = await _context.Projects.ToListAsync();
            ViewBag.AssignedProjectIds = selectedProjectIds;
            return View(department);
        }

        // 7. GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FirstOrDefaultAsync(m => m.Id == id);
            if (department == null) return NotFound();

            return View(department);
        }

        // 8. POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.Include(d => d.Projects).FirstOrDefaultAsync(d => d.Id == id);
            if (department != null)
            {
                // فك ارتباط المشاريع بهذا القسم قبل الحذف لتجنب أخطاء الـ Foreign Key
                foreach (var project in department.Projects)
                {
                    project.DepartmentId = null;
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}