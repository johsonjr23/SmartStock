using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartStockDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            SmartStockDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1. Create Tenant
            var tenant = new Tenant
            {
                Name = model.ShopName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            // 2. Create User linked to Tenant
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // 3. Auto login
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
