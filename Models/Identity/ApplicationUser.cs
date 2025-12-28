using Microsoft.AspNetCore.Identity;

namespace SmartStock.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Every user MUST belong to exactly one tenant (shop)
        public int TenantId { get; set; }
    }
}
