using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers
{
    [Authorize(Policy = "Admin")]
    public class ProductController : Controller
    {
        private readonly EcommerceContext _dbcontext;

        public ProductController(EcommerceContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // GET: Products
        public IActionResult Index()
        {
            var products = _dbcontext.Products.Include(p => p.Department).ToList();
            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewBag.Departments = _dbcontext.Departments.ToList();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _dbcontext.Products.Add(product);
                _dbcontext.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = _dbcontext.Departments.ToList();
            return View(product);
        }

        // GET: Products/Edit/5
        public IActionResult Edit(int id)
        {
            var product = _dbcontext.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Departments = _dbcontext.Departments.ToList();
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                _dbcontext.Products.Update(product);
                _dbcontext.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = _dbcontext.Departments.ToList();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _dbcontext.Products.Find(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            _dbcontext.Products.Remove(product);
            _dbcontext.SaveChanges();
            return Json(new { success = true });
        }
    }
}
