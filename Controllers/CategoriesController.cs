using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;

namespace SmartStock.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly SmartStockDbContext _context;

        public CategoriesController(SmartStockDbContext context)
        {
            _context = context;
        }

        // LIST CATEGORIES
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        // SHOW CREATE FORM
        public IActionResult Create()
        {
            return View();
        }

        // SAVE CATEGORY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            // IMPORTANT: remove tenant validation
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            // assign tenant manually (temporary)
            category.TenantId = 1;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
