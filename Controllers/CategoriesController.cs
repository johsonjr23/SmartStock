using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartStock.Data;
using SmartStock.Models;

namespace SmartStock.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly SmartStockDbContext _context;

        public CategoriesController(
            SmartStockDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(userManager)
        {
            _context = context;
        }

        // LIST CATEGORIES
        public async Task<IActionResult> Index()
        {
            var tenantId = GetTenantId();

            var categories = await _context.Categories
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Name)
                .ToListAsync();

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
            var tenantId = GetTenantId();

            // Tenant is always set server-side
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.TenantId = tenantId;
            category.CreatedAt = DateTime.UtcNow;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            //return RedirectToAction(nameof(Index));
            return RedirectToAction("Create", "Products");

        }
    }
}
