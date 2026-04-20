using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.Services;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartStockDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _email;

        public AccountController(
            SmartStockDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService email)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _email = email;
        }

        // =========================
        // REQUEST SHOP
        // =========================
        [HttpGet]
        public IActionResult RequestShop() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestShop(RequestShopViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.TenantRequests.Add(new TenantRequest
            {
                ShopName     = model.ShopName.Trim(),
                OwnerName    = model.OwnerName.Trim(),
                Email        = model.Email.Trim(),
                Phone        = model.Phone?.Trim(),
                BusinessType = model.BusinessType?.Trim(),
                Message      = model.Message?.Trim(),
                Status       = "Pending",
                CreatedAt    = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(RequestShopConfirmation));
        }

        [HttpGet]
        public IActionResult RequestShopConfirmation() => View();

        [HttpGet]
        public IActionResult Register() => RedirectToAction(nameof(RequestShop));

        // =========================
        // LOGIN  (step 1 of 2)
        // =========================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Look up the user first so we can do pre-checks
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                // Block login for suspended tenants before wasting any further work
                if (!await _userManager.IsInRoleAsync(user, "SystemAdmin") && user.TenantId != null)
                {
                    var tenant = await _db.Tenants.FindAsync(user.TenantId.Value);
                    if (tenant != null && !tenant.IsActive)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Your account has been suspended. Please contact the system administrator.");
                        return View(model);
                    }
                }

                // 2FA only for non-SystemAdmin users
                if (await _userManager.IsInRoleAsync(user, "SystemAdmin"))
                {
                    // Ensure SystemAdmin always bypasses 2FA
                    if (user.TwoFactorEnabled)
                        await _userManager.SetTwoFactorEnabledAsync(user, false);
                }
                else
                {
                    if (!user.TwoFactorEnabled)
                        await _userManager.SetTwoFactorEnabledAsync(user, true);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                // Generate a 6-digit code via Identity's email provider and send it
                if (user != null)
                {
                    var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                    try
                    {
                        await _email.SendTwoFactorCodeAsync(
                            user.Email!, user.FullName ?? user.Email!, code);
                    }
                    catch
                    {
                        // Email failed — still show verify page; user can resend
                    }
                }

                return RedirectToAction(nameof(VerifyCode), new { rememberMe = model.RememberMe });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked. Please try again in 15 minutes.");
                return View(model);
            }

            // result.Succeeded only lands here if 2FA is somehow bypassed (shouldn't happen)
            if (result.Succeeded)
                return await RedirectAfterLogin(user!);

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // =========================
        // VERIFY CODE  (step 2 of 2)
        // =========================
        [HttpGet]
        public async Task<IActionResult> VerifyCode(bool rememberMe = false)
        {
            // Guard: only accessible when a 2FA session is in progress
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToAction(nameof(Login));

            // Mask email: j***@example.com
            var email = user.Email ?? "";
            var atIdx = email.IndexOf('@');
            var masked = atIdx > 1
                ? email[0] + new string('*', atIdx - 1) + email[atIdx..]
                : email;

            ViewBag.MaskedEmail = masked;
            return View(new VerifyCodeViewModel { RememberMe = rememberMe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid) return await ReturnVerifyView(model);

            // Grab user BEFORE completing sign-in (needed for role-based redirect)
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToAction(nameof(Login));

            var result = await _signInManager.TwoFactorSignInAsync(
                "Email", model.Code.Trim(), model.RememberMe, rememberClient: false);

            if (result.Succeeded)
                return await RedirectAfterLogin(user);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked. Please try again in 15 minutes.");
                return await ReturnVerifyView(model);
            }

            ModelState.AddModelError(string.Empty, "Incorrect or expired code. Please try again.");
            return await ReturnVerifyView(model);
        }

        // =========================
        // RESEND CODE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode(bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToAction(nameof(Login));

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            try
            {
                await _email.SendTwoFactorCodeAsync(user.Email!, user.FullName ?? user.Email!, code);
                TempData["ResendSuccess"] = "A new code has been sent to your email.";
            }
            catch
            {
                TempData["ResendError"] = "Could not send the code. Please try again.";
            }

            return RedirectToAction(nameof(VerifyCode), new { rememberMe });
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // =========================
        // SUSPENDED
        // =========================
        [HttpGet]
        public async Task<IActionResult> Suspended()
        {
            if (User.Identity?.IsAuthenticated == true)
                await _signInManager.SignOutAsync();
            return View();
        }

        // =========================
        // ACCESS DENIED
        // =========================
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ── helpers ─────────────────────────────────────────────
        private async Task<IActionResult> RedirectAfterLogin(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "SystemAdmin"))
                return RedirectToAction("Index", "Tenants", new { area = "SystemAdmin" });

            return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> ReturnVerifyView(VerifyCodeViewModel model)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            var email = user?.Email ?? "";
            var atIdx = email.IndexOf('@');
            ViewBag.MaskedEmail = atIdx > 1
                ? email[0] + new string('*', atIdx - 1) + email[atIdx..]
                : email;
            return View(model);
        }
    }
}
