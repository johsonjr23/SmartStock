using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using System.Security.Claims;

namespace SmartStock.Controllers.Api
{
    [Route("api/sales")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Roles = "TenantAdmin,Manager,Cashier")]
    public class SalesApiController : ControllerBase
    {
        private readonly SmartStockDbContext _db;

        public SalesApiController(SmartStockDbContext db) => _db = db;

        // POST /api/sales
        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleApiRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest(new { error = "At least one item is required." });

            var tenantId = GetTenantId();

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var sale = new Sale
                {
                    TenantId = tenantId,
                    TotalAmount = 0m,
                    Status = SaleStatus.Completed,
                    CompletedAt = DateTime.UtcNow
                };
                _db.Sales.Add(sale);
                await _db.SaveChangesAsync();

                decimal total = 0m;

                foreach (var item in req.Items.OrderBy(i => i.ProductId))
                {
                    if (item.Quantity <= 0)
                        throw new Exception("Quantity must be at least 1.");

                    await LockProductAsync(tenantId, item.ProductId);

                    var product = await _db.Products
                        .Where(p => p.TenantId == tenantId && p.IsActive && p.Id == item.ProductId)
                        .FirstOrDefaultAsync()
                        ?? throw new Exception($"Product #{item.ProductId} not found or inactive.");

                    var stock = CurrentStock(item.ProductId, tenantId);
                    if (item.Quantity > stock)
                        throw new Exception($"Insufficient stock for {product.Name}. Available: {stock}");

                    var lineTotal = product.SellingPrice * item.Quantity;
                    total += lineTotal;

                    _db.SaleItems.Add(new SaleItem
                    {
                        SaleId = sale.Id,
                        TenantId = tenantId,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        SellingPrice = product.SellingPrice,
                        BuyingPriceAtSale = product.BuyingPrice,
                        SubTotal = lineTotal
                    });

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TenantId = tenantId,
                        ProductId = product.Id,
                        SaleId = sale.Id,
                        QuantityChange = -item.Quantity,
                        TransactionType = InventoryTransactionType.Sale
                    });
                }

                sale.TotalAmount = total;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    saleId = sale.Id,
                    invoiceNumber = string.IsNullOrWhiteSpace(sale.InvoiceNumber)
                        ? $"SALE-{sale.Id:D6}"
                        : sale.InvoiceNumber,
                    totalAmount = total,
                    completedAt = sale.CompletedAt
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET /api/sales?page=1&pageSize=30
        [HttpGet]
        public async Task<IActionResult> GetSales([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            var tenantId = GetTenantId();

            var sales = await _db.Sales
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    InvoiceNumber = string.IsNullOrWhiteSpace(s.InvoiceNumber)
                        ? $"SALE-{s.Id:D6}"
                        : s.InvoiceNumber,
                    s.TotalAmount,
                    Status = s.Status.ToString(),
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(sales);
        }

        // ── helpers ──────────────────────────────────────────────────────

        private int GetTenantId()
        {
            var claim = User.FindFirstValue("tenantId");
            if (int.TryParse(claim, out var id)) return id;
            throw new UnauthorizedAccessException("TenantId missing from token.");
        }

        private int CurrentStock(int productId, int tenantId) =>
            _db.InventoryTransactions
                .Where(t => t.TenantId == tenantId && t.ProductId == productId)
                .Sum(t => t.QuantityChange);

        private Task LockProductAsync(int tenantId, int productId) =>
            _db.Database.ExecuteSqlRawAsync(@"
SELECT 1 FROM Products WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE TenantId = {0} AND Id = {1}", tenantId, productId);
    }

    public class CreateSaleApiRequest
    {
        public List<SaleItemInput> Items { get; set; } = [];
    }

    public class SaleItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
