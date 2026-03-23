using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;
using TenantEntity = SmartStock.Models.Tenant;

namespace SmartStock.Areas.SystemAdmin.Controllers
{
    [Area("SystemAdmin")]
    [Authorize(Roles = "SystemAdmin")]
    public class TenantsController : Controller
    {
        private readonly SmartStockDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantsController(SmartStockDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // GET: /SystemAdmin/Tenants
        // =========================
        public async Task<IActionResult> Index()
        {
            var tenants = await _db.Tenants
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tenants);
        }

        // =========================
        // GET: /SystemAdmin/Tenants/Create
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateTenantViewModel());
        }

        // =========================
        // POST: /SystemAdmin/Tenants/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTenantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check email is not already taken
            if (await _userManager.FindByEmailAsync(model.AdminEmail) != null)
            {
                ModelState.AddModelError("AdminEmail", "This email is already in use.");
                return View(model);
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // 1. Create Tenant
                var tenant = new TenantEntity
                {
                    Name = model.BusinessName.Trim(),
                    OwnerName = model.OwnerName?.Trim(),
                    Phone = model.Phone?.Trim(),
                    Email = model.AdminEmail.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync();

                // 2. Create TenantAdmin user
                var user = new ApplicationUser
                {
                    UserName = model.AdminEmail.Trim(),
                    Email = model.AdminEmail.Trim(),
                    FullName = model.OwnerName?.Trim() ?? model.BusinessName.Trim(),
                    TenantId = tenant.Id,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.AdminPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    await tx.RollbackAsync();
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "TenantAdmin");

                await tx.CommitAsync();

                TempData["Success"] = $"Tenant '{tenant.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // =========================
        // POST: /SystemAdmin/Tenants/Disable/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(int id)
        {
            var tenant = await _db.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            tenant.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Tenant '{tenant.Name}' has been disabled.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // POST: /SystemAdmin/Tenants/Enable/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(int id)
        {
            var tenant = await _db.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            tenant.IsActive = true;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Tenant '{tenant.Name}' has been enabled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
