using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Models;
using System.Security.Claims;

namespace SmartStock.Controllers
{
    [Authorize] // 🔒 Enforce login for all derived controllers
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected int GetTenantId()
        {
            // User is guaranteed to be authenticated because of [Authorize]
            var userId = _userManager.GetUserId(User);

            // Extra safety (should not normally happen)
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
