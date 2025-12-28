using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SmartStock.Models;

namespace SmartStock.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected int GetTenantId()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                throw new Exception("User not logged in");

            var user = _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.TenantId })
                .First();

            return user.TenantId;
        }
    }
}
