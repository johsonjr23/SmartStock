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
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            SmartStockDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // =========================
        // REGISTER
        // =========================
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

            // 1️⃣ Create Tenant (Shop)
            var tenant = new Tenant
            {
                Name = model.ShopName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(); // MUST happen first

            // 2️⃣ Create User linked to Tenant
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            // Rollback tenant if user creation fails
            if (!result.Succeeded)
            {
                _db.Tenants.Remove(tenant);
                await _db.SaveChangesAsync();

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // Assign TenantAdmin role
            await _userManager.AddToRoleAsync(user, "TenantAdmin");

            // Auto-login after registration
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                var loggedInUser = await _userManager.FindByEmailAsync(model.Email);
                if (loggedInUser != null && await _userManager.IsInRoleAsync(loggedInUser, "SystemAdmin"))
                    return RedirectToAction("Index", "Tenants", new { area = "SystemAdmin" });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // =========================
        // ACCESS DENIED
        // =========================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
