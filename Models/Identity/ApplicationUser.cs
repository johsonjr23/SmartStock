using Microsoft.AspNetCore.Identity;

namespace SmartStock.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Null for SystemAdmin; required for all tenant users
        public int? TenantId { get; set; }

        public string? FullName { get; set; }
    }
}
