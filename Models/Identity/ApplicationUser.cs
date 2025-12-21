using Microsoft.AspNetCore.Identity;

namespace SmartStock.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int TenantId { get; set; }
    }
}
