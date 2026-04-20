using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartStock.Data;
using SmartStock.Models;
using System.Security.Claims;

namespace SmartStock.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // SystemAdmin has no tenant — skip check
            if (User.IsInRole("SystemAdmin")) return;
            if (User.Identity?.IsAuthenticated != true) return;

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return;

            var db = HttpContext.RequestServices.GetRequiredService<SmartStockDbContext>();

            var tenantId = _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.TenantId)
                .FirstOrDefault();

            if (tenantId == null) return;

            var tenant = db.Tenants.Find(tenantId.Value);
            if (tenant != null && !tenant.IsActive)
                context.Result = RedirectToAction("Suspended", "Account");
        }

        protected int GetTenantId()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var tenantId = _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.TenantId)
                .FirstOrDefault();

            if (tenantId == null)
                throw new Exception("Tenant not assigned to this user account.");

            return tenantId.Value;
        }
    }
}
