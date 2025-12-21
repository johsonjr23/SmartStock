using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartStock.Models;

namespace SmartStock.Data
{
    public class SmartStockIdentityDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public SmartStockIdentityDbContext(
            DbContextOptions<SmartStockIdentityDbContext> options)
            : base(options)
        {
        }
    }
}
