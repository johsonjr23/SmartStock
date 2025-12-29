using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using SmartStock.Data;
using SmartStock.Models;

namespace SmartStock.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly SmartStockDbContext _context;

        public ProductsController(
            SmartStockDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(userManager)
        {
            _context = context;
        }

        // =========================
        // LIST PRODUCTS
        // =========================
        public async Task<IActionResult> Index()
        {
            var tenantId = GetTenantId();

            var products = await _context.Products
                .Where(p => p.TenantId == tenantId)
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .ToListAsync();

            var stockDict = products.ToDictionary(
                p => p.Id,
                p => GetCurrentStock(p.Id, tenantId)
            );

            ViewBag.Stock = stockDict;
            return View(products);
        }

        // =========================
        // SHOW CREATE FORM
        // =========================
        public IActionResult Create()
        {
            var tenantId = GetTenantId();

            PopulateDropdowns(tenantId);
            return View();
        }

        // =========================
        // SAVE PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            var tenantId = GetTenantId();

            ModelState.Remove(nameof(Product.Tenant));
            ModelState.Remove(nameof(Product.TenantId));

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(tenantId, product.CategoryId, product.UnitId);
                return View(product);
            }

            product.TenantId = tenantId;
            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // UX FLOW: product → stock in
            return RedirectToAction(nameof(StockIn), new { productId = product.Id });
        }

        // =========================
        // SHOW EDIT FORM
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            PopulateDropdowns(tenantId, product.CategoryId, product.UnitId);
            return View(product);
        }

        // =========================
        // SAVE EDITED PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            var tenantId = GetTenantId();

            if (id != product.Id)
                return NotFound();

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (existingProduct == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(tenantId, product.CategoryId, product.UnitId);
                return View(product);
            }

            existingProduct.Name = product.Name;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.UnitId = product.UnitId;
            existingProduct.SellingPrice = product.SellingPrice;
            existingProduct.SKU = product.SKU;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // SHOW DELETE CONFIRMATION
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =========================
        // PERFORM DELETE
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .Include(p => p.SaleItems)
                .Include(p => p.PurchaseItems)
                .Include(p => p.StockHistories)
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            if (product.SaleItems.Any() ||
                product.PurchaseItems.Any() ||
                product.StockHistories.Any())
            {
                TempData["DeleteError"] =
                    "This product cannot be deleted because it has sales, purchases, or stock history.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // PRODUCT DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =========================
        // STOCK IN (GET)
        // =========================
        public IActionResult StockIn(int id)
        {
            var tenantId = GetTenantId();

            var product = _context.Products
               .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =========================
        // STOCK IN (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                ModelState.AddModelError("", "Quantity must be greater than zero.");
                return RedirectToAction(nameof(StockIn), new { productId });
            }

            var tenantId = GetTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                QuantityChange = quantity,
                TransactionType = InventoryTransactionType.Purchase,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // UX FLOW: stock in → back to products
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private int GetCurrentStock(int productId, int tenantId)
        {
            return _context.InventoryTransactions
                .Where(t => t.ProductId == productId && t.TenantId == tenantId)
                .Sum(t => t.QuantityChange);
        }

        private void PopulateDropdowns(int tenantId, int? categoryId = null, int? unitId = null)
        {
            ViewData["Categories"] = new SelectList(
                _context.Categories.Where(c => c.TenantId == tenantId),
                "Id", "Name", categoryId
            );

            ViewData["Units"] = new SelectList(
                _context.Units,
                "Id", "Name", unitId
            );
        }
    }
}
