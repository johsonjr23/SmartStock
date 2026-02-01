using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    public class SalesController : BaseController
    {
        private readonly SmartStockDbContext _context;

        public SalesController(
            SmartStockDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(userManager)
        {
            _context = context;
        }

        // =========================
        // GET: /Sales/New
        // =========================
        [HttpGet]
        public IActionResult New()
        {
            var vm = new CreateSaleViewModel();
            LoadProducts();
            return View(vm);
        }

        // =========================
        // POST: /Sales/New
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(
            CreateSaleViewModel model,
            string action,
            int? removeIndex)
        {
            var tenantId = GetTenantId();

            model.Items ??= new();

            // -------------------------
            // ADD PRODUCT
            // -------------------------
            if (action == "add")
            {
                if (string.IsNullOrWhiteSpace(model.ProductSearch))
                {
                    ModelState.AddModelError("", "Type a product name or SKU");
                    RefreshAvailableStock(model, tenantId);
                    LoadProducts();
                    return View(model);
                }

                var search = model.ProductSearch.Trim();

                var product = await _context.Products
                    .Where(p => p.TenantId == tenantId)
                    .Where(p => p.IsActive)
                    .Where(p =>
                        p.Name.Contains(search) ||
                        (p.SKU != null && p.SKU.Contains(search)))
                    .OrderBy(p => p.Name)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    ModelState.AddModelError("", "Product not found");
                    RefreshAvailableStock(model, tenantId);
                    LoadProducts();
                    return View(model);
                }

                var existingItem = model.Items.FirstOrDefault(i => i.ProductId == product.Id);

                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    model.Items.Add(new SaleItemInputViewModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.SellingPrice, // display only
                        Quantity = 1
                    });
                }

                model.ProductSearch = "";

                RefreshAvailableStock(model, tenantId);
                LoadProducts();
                return View(model);
            }

            // -------------------------
            // REMOVE ITEM
            // -------------------------
            if (removeIndex.HasValue &&
                removeIndex.Value >= 0 &&
                removeIndex.Value < model.Items.Count)
            {
                model.Items.RemoveAt(removeIndex.Value);

                RefreshAvailableStock(model, tenantId);
                LoadProducts();
                return View(model);
            }

            // -------------------------
            // COMPLETE SALE (ATOMIC CHECKOUT)
            // -------------------------
            if (action == "complete")
            {
                if (model.Items.Count == 0)
                {
                    ModelState.AddModelError("", "Add at least one item");
                    RefreshAvailableStock(model, tenantId);
                    LoadProducts();
                    return View(model);
                }

                if (!TryValidateModel(model))
                {
                    RefreshAvailableStock(model, tenantId);
                    LoadProducts();
                    return View(model);
                }

                await using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    var sale = new Sale
                    {
                        TenantId = tenantId,
                        TotalAmount = 0m,
                        Status = SaleStatus.Completed,
                        CompletedAt = DateTime.UtcNow
                    };

                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    decimal total = 0m;

                    foreach (var item in model.Items.OrderBy(i => i.ProductId))
                    {
                        if (item.Quantity <= 0)
                            throw new Exception("Quantity must be at least 1.");

                        await LockProductRowAsync(tenantId, item.ProductId);

                        var product = await _context.Products
                            .Where(p => p.TenantId == tenantId && p.IsActive)
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        if (product == null)
                            throw new Exception("Product not found or inactive");

                        var currentStock = GetCurrentStock(product.Id, tenantId);

                        if (item.Quantity > currentStock)
                            throw new Exception($"Not enough stock for {product.Name}. Available: {currentStock}");

                        var unitPrice = product.SellingPrice;
                        var lineTotal = unitPrice * item.Quantity;

                        _context.SaleItems.Add(new SaleItem
                        {
                            SaleId = sale.Id,
                            TenantId = tenantId,
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            SellingPrice = unitPrice,

                            // ✅ Phase 2 snapshot
                            BuyingPriceAtSale = product.BuyingPrice,

                            SubTotal = lineTotal
                        });

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TenantId = tenantId,
                            ProductId = product.Id,
                            SaleId = sale.Id,
                            QuantityChange = -item.Quantity,
                            TransactionType = InventoryTransactionType.Sale
                        });

                        total += lineTotal;
                    }

                    sale.TotalAmount = total;
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                    TempData["Success"] = "Sale completed successfully";

                    return RedirectToAction("Details", new { id = sale.Id });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);

                    RefreshAvailableStock(model, tenantId);
                    LoadProducts();
                    return View(model);
                }
            }

            // Default fallback
            RefreshAvailableStock(model, tenantId);
            LoadProducts();
            return View(model);
        }

        // =========================
        // STEP 4: /Sales/Details/{id}
        // =========================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var tenantId = GetTenantId();

            var sale = await _context.Sales
                .Where(s => s.TenantId == tenantId && s.Id == id)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync();

            if (sale == null)
                return NotFound();

            var vm = new SaleDetailsViewModel
            {
                Id = sale.Id,
                DisplayInvoiceNumber = !string.IsNullOrWhiteSpace(sale.InvoiceNumber)
                    ? sale.InvoiceNumber!
                    : $"SALE-{sale.Id:D6}",

                SaleDate = sale.CreatedAt,
                PaymentType = "N/A",
                TotalAmount = sale.TotalAmount,

                Status = sale.Status.ToString(),
                VoidedAt = sale.VoidedAt,
                VoidReason = sale.VoidReason,

                Items = sale.SaleItems
                    .OrderBy(i => i.Id)
                    .Select(i => new SaleDetailsViewModel.SaleDetailsItemRow
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : $"Product #{i.ProductId}",
                        SKU = i.Product != null ? i.Product.SKU : null,
                        Quantity = i.Quantity,
                        UnitPrice = i.SellingPrice
                    })
                    .ToList()
            };

            return View(vm);
        }

        // =========================
        // STEP 5.2: VOID SALE
        // POST: /Sales/Void/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Void(int id, string reason)
        {
            var tenantId = GetTenantId();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Void reason is required.";
                return RedirectToAction("Details", new { id });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Where(s => s.TenantId == tenantId && s.Id == id)
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync();

                if (sale == null)
                    return NotFound();

                if (sale.Status != SaleStatus.Completed)
                {
                    TempData["Error"] = $"Sale cannot be voided because it is {sale.Status}.";
                    return RedirectToAction("Details", new { id });
                }

                sale.Status = SaleStatus.Voided;
                sale.VoidedAt = DateTime.UtcNow;
                sale.VoidReason = reason.Trim();

                foreach (var item in sale.SaleItems)
                {
                    await LockProductRowAsync(tenantId, item.ProductId);

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TenantId = tenantId,
                        ProductId = item.ProductId,
                        SaleId = sale.Id,
                        QuantityChange = item.Quantity,
                        TransactionType = InventoryTransactionType.Void
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Sale voided successfully.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // =========================
        // STEP 5.3: FULL REFUND
        // POST: /Sales/RefundFull/{id}
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundFull(int id, string reason)
        {
            var tenantId = GetTenantId();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Refund reason is required.";
                return RedirectToAction("Details", new { id });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var original = await _context.Sales
                    .Where(s => s.TenantId == tenantId && s.Id == id)
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync();

                if (original == null)
                    return NotFound();

                if (original.Status == SaleStatus.Voided)
                {
                    TempData["Error"] = "Cannot refund a voided sale.";
                    return RedirectToAction("Details", new { id });
                }

                if (original.Status == SaleStatus.Refunded)
                {
                    TempData["Error"] = "Sale is already refunded.";
                    return RedirectToAction("Details", new { id });
                }

                if (original.Status != SaleStatus.Completed)
                {
                    TempData["Error"] = $"Sale cannot be refunded because it is {original.Status}.";
                    return RedirectToAction("Details", new { id });
                }

                var refundSale = new Sale
                {
                    TenantId = tenantId,
                    TotalAmount = 0m,
                    Status = SaleStatus.Refunded,
                    CompletedAt = DateTime.UtcNow,

                    OriginalSaleId = original.Id,
                    RefundedAt = DateTime.UtcNow,
                    RefundReason = reason.Trim()
                };

                _context.Sales.Add(refundSale);
                await _context.SaveChangesAsync();

                decimal refundTotal = 0m;

                foreach (var item in original.SaleItems.OrderBy(i => i.ProductId))
                {
                    await LockProductRowAsync(tenantId, item.ProductId);

                    // Fallback safety for old historical rows (pre Phase 2)
                    var product = await _context.Products
                        .Where(p => p.TenantId == tenantId && p.IsActive)
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null)
                        throw new Exception("Product not found or inactive");

                    var negativePrice = -item.SellingPrice;
                    var lineTotal = negativePrice * item.Quantity;

                    var costSnapshot = item.BuyingPriceAtSale ?? product.BuyingPrice;

                    _context.SaleItems.Add(new SaleItem
                    {
                        SaleId = refundSale.Id,
                        TenantId = tenantId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        SellingPrice = negativePrice,

                        // ✅ Phase 2 snapshot carried into refund
                        BuyingPriceAtSale = costSnapshot,

                        SubTotal = lineTotal
                    });

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TenantId = tenantId,
                        ProductId = item.ProductId,
                        SaleId = refundSale.Id,
                        QuantityChange = item.Quantity,
                        TransactionType = InventoryTransactionType.Refund
                    });

                    refundTotal += lineTotal;
                }

                refundSale.TotalAmount = refundTotal;

                original.Status = SaleStatus.Refunded;
                original.RefundedAt = refundSale.RefundedAt;
                original.RefundReason = refundSale.RefundReason;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Full refund completed successfully.";
                return RedirectToAction("Details", new { id = refundSale.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // =========================
        // AUTOCOMPLETE ENDPOINT
        // =========================
        [HttpGet]
        public IActionResult SearchProducts(string term)
        {
            var tenantId = GetTenantId();

            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var results = _context.Products
                .Where(p => p.TenantId == tenantId)
                .Where(p => p.IsActive)
                .Where(p =>
                    p.Name.Contains(term) ||
                    (p.SKU != null && p.SKU.Contains(term)))
                .OrderBy(p => p.Name)
                .Take(8)
                .Select(p => new
                {
                    id = p.Id,
                    label = p.Name + " (TZS " + p.SellingPrice.ToString("N0") + ")",
                    value = p.Name
                })
                .ToList();

            return Json(results);
        }

        // =========================
        // GET: /Sales
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tenantId = GetTenantId();

            var sales = await _context.Sales
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.Id)
                .Take(200)
                .ToListAsync();

            return View(sales);
        }

        // =========================
        // HELPERS
        // =========================
        private void LoadProducts()
        {
            var tenantId = GetTenantId();

            ViewBag.Products = _context.Products
                .Where(p => p.TenantId == tenantId)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToList();
        }

        private int GetCurrentStock(int productId, int tenantId)
        {
            return _context.InventoryTransactions
                .Where(t => t.TenantId == tenantId)
                .Where(t => t.ProductId == productId)
                .Sum(t => t.QuantityChange);
        }

        private async Task LockProductRowAsync(int tenantId, int productId)
        {
            await _context.Database.ExecuteSqlRawAsync(@"
SELECT 1
FROM Products WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE TenantId = {0} AND Id = {1}", tenantId, productId);
        }

        private void RefreshAvailableStock(CreateSaleViewModel model, int tenantId)
        {
            if (model.Items == null) return;

            foreach (var item in model.Items)
            {
                item.AvailableStock = GetCurrentStock(item.ProductId, tenantId);
            }
        }
    }
}
