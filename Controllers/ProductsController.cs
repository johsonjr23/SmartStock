using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

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

        public async Task<IActionResult> Index(string search, int page = 1)
        {
            const int PageSize = 20;

            var tenantId = GetTenantId();

            var query = _context.Products
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
        (p.Name != null && p.Name.Contains(search)) ||
        (p.SKU != null && p.SKU.Contains(search))
    );
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Name)              // REQUIRED for stable pagination
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalCount / (double)PageSize);

            ViewBag.Search = search;

            ViewBag.Stock = products.ToDictionary(
                p => p.Id,
                p => GetCurrentStock(p.Id, tenantId)
            );

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
        public async Task<IActionResult> Create(Product product, int? openingStock)
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

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // OPENING STOCK LOGIC
                if (openingStock.HasValue && openingStock.Value > 0)
                {
                    var inventoryTransaction = new InventoryTransaction
                    {
                        ProductId = product.Id,
                        TenantId = tenantId,
                        QuantityChange = openingStock.Value,
                        TransactionType = InventoryTransactionType.Opening,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InventoryTransactions.Add(inventoryTransaction);
                    await _context.SaveChangesAsync();
                }

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(Index));
        
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
        public IActionResult Edit(int id, Product model)
        {
            var tenantId = GetTenantId();

            ModelState.Remove(nameof(Product.Tenant));
            ModelState.Remove(nameof(Product.TenantId));

            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(tenantId, model.CategoryId, model.UnitId);
                return View(model);
            }

            var product = _context.Products
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            product.Name = model.Name;
            product.CategoryId = model.CategoryId;
            product.SKU = model.SKU;
            product.BuyingPrice = model.BuyingPrice;
            product.SellingPrice = model.SellingPrice;
            product.UnitId = model.UnitId; // ✅ FIX
            product.ReorderLevel = model.ReorderLevel;

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }



        // =========================
        // SHOW DELETE CONFIRMATION
        // =========================
      

        // =========================
        // PERFORM DELETE
        // =========================
       

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

        public IActionResult AdjustStock(int id)
        {
            var tenantId = GetTenantId();

            var product = _context.Products
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            var vm = new AdjustStockViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(AdjustStockViewModel vm)
        {
            var tenantId = GetTenantId();

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.Quantity == 0)
            {
                ModelState.AddModelError("", "Adjustment quantity cannot be zero.");
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.Reason))
            {
                ModelState.AddModelError("", "Reason is required for stock adjustment.");
                return View(vm);
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == vm.ProductId && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            var adjustment = new InventoryTransaction
            {
                ProductId = product.Id,
                TenantId = tenantId,
                QuantityChange = vm.Quantity,
                TransactionType = InventoryTransactionType.Adjustment,
                Reason = vm.Reason
            };

            _context.InventoryTransactions.Add(adjustment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = product.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            product.IsActive = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var tenantId = GetTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (product == null)
                return NotFound();

            product.IsActive = true;
            await _context.SaveChangesAsync();


            //return RedirectToAction(nameof(Index));

            TempData["Success"] = "Product activated successfully.";
            return RedirectToAction(nameof(Inactive));

        }
        public async Task<IActionResult> Inactive(string search)
        {
            var tenantId = GetTenantId();

            var query = _context.Products
                .Where(p => p.TenantId == tenantId && !p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
        (p.Name != null && p.Name.Contains(search)) ||
        (p.SKU != null && p.SKU.Contains(search))
    );
            }

            var products = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.Search = search;

            return View(products);
        }



    }
}
