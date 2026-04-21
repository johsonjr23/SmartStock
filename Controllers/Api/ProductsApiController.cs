using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using System.Security.Claims;

namespace SmartStock.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Roles = "TenantAdmin,Manager,Cashier")]
    public class ProductsApiController : ControllerBase
    {
        private readonly SmartStockDbContext _db;

        public ProductsApiController(SmartStockDbContext db) => _db = db;

        // GET /api/products?search=...
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            var tenantId = GetTenantId();

            var query = _db.Products
                .Where(p => p.TenantId == tenantId && p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.SKU != null && p.SKU.Contains(search)));

            var products = await query
                .OrderBy(p => p.Name)
                .Take(50)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.SellingPrice,
                    Category = p.Category != null ? p.Category.Name : null
                })
                .ToListAsync();

            var ids = products.Select(p => p.Id).ToList();
            var stockMap = _db.InventoryTransactions
                .Where(t => t.TenantId == tenantId && ids.Contains(t.ProductId))
                .GroupBy(t => t.ProductId)
                .Select(g => new { ProductId = g.Key, Stock = g.Sum(t => t.QuantityChange) })
                .ToDictionary(x => x.ProductId, x => x.Stock);

            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.SellingPrice,
                p.Category,
                Stock = stockMap.TryGetValue(p.Id, out var s) ? s : 0
            });

            return Ok(result);
        }

        private int GetTenantId()
        {
            var claim = User.FindFirstValue("tenantId");
            if (int.TryParse(claim, out var id)) return id;
            throw new UnauthorizedAccessException("TenantId missing from token.");
        }
    }
}
