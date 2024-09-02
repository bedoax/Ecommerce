using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    [Authorize(Policy = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly EcommerceContext _dbContext;

        public DepartmentController(EcommerceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Departments
        public IActionResult Index()
        {
            var departments = _dbContext.Departments.ToList();
            var departmentProductCounts = _dbContext.Products
                                                  .Join(
                                                      _dbContext.Departments,
                                                      p => p.DepartmentId,
                                                      d => d.Id,
                                                      (p, d) => new { d.Id, d.Name }
                                                  )
                                                  .GroupBy(pd => pd.Id)
                                                  .ToDictionary(
                                                      g => g.Key,
                                                      g => g.Count()
                                                  );
            ViewBag.DepartmentProductCounts = departmentProductCounts;
            return View(departments);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Departments.Add(department);
                _dbContext.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Departments/Edit/5
        public IActionResult Edit(int id)
        {
            var department = _dbContext.Departments.Find(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Department department)
        {
            if (id != department.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Departments.Update(department);
                    _dbContext.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_dbContext.Departments.Any(d => d.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var department = _dbContext.Departments.Find(id);
            if (department == null)
            {
                return Json(new { success = false, message = "Department not found" });
            }

            _dbContext.Departments.Remove(department);
            _dbContext.SaveChanges();
            return Json(new { success = true });
        }
    }
}
