using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize(Roles = "TenantAdmin")]
    public class UsersController : BaseController
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        ) : base(userManager)
        {
            _roleManager = roleManager;
        }

        // =========================
        // GET: /Users
        // =========================
        public async Task<IActionResult> Index()
        {
            var tenantId = GetTenantId();

            var users = _userManager.Users
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.FullName)
                .ToList();

            // Build role lookup
            var userRoles = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRoles[u.Id] = roles.FirstOrDefault() ?? "-";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // =========================
        // GET: /Users/Create
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        // =========================
        // POST: /Users/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Only allow Manager or Cashier roles
            if (model.Role != "Manager" && model.Role != "Cashier")
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
                return View(model);
            }

            var tenantId = GetTenantId();

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                TenantId = tenantId,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["Success"] = $"User '{user.FullName}' created as {model.Role}.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // POST: /Users/Disable/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            var tenantId = GetTenantId();
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.TenantId != tenantId)
                return NotFound();

            // Prevent TenantAdmin from disabling themselves
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot disable your own account.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            TempData["Success"] = $"User '{user.FullName ?? user.Email}' has been disabled.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // POST: /Users/Enable/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            var tenantId = GetTenantId();
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.TenantId != tenantId)
                return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);

            TempData["Success"] = $"User '{user.FullName ?? user.Email}' has been enabled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
