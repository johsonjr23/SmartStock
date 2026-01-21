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
                    // Don't set AvailableStock here; we refresh for all items below
                }
                else
                {
                    model.Items.Add(new SaleItemInputViewModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.SellingPrice, // display
                        Quantity = 1
                        // AvailableStock will be set by RefreshAvailableStock below
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
                        TotalAmount = 0m
                    };

                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    decimal total = 0m;

                    foreach (var item in model.Items.OrderBy(i => i.ProductId))
                    {
                        await LockProductRowAsync(tenantId, item.ProductId);

                        var product = await _context.Products
                            .Where(p => p.TenantId == tenantId && p.IsActive)
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        if (product == null)
                            throw new Exception("Product not found or inactive");

                        var currentStock = GetCurrentStock(product.Id, tenantId);

                        if (item.Quantity > currentStock)
                            throw new Exception(
                                $"Not enough stock for {product.Name}. Available: {currentStock}");

                        var unitPrice = product.SellingPrice;

                        _context.SaleItems.Add(new SaleItem
                        {
                            SaleId = sale.Id,
                            TenantId = tenantId,
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            SellingPrice = unitPrice
                        });

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TenantId = tenantId,
                            ProductId = product.Id,
                            SaleId = sale.Id,
                            QuantityChange = -item.Quantity,
                            TransactionType = InventoryTransactionType.Sale
                        });

                        total += unitPrice * item.Quantity;
                    }

                    sale.TotalAmount = total;
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                    TempData["Success"] = "Sale completed successfully";
                    return RedirectToAction("New");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);

                    // IMPORTANT: keep AvailableStock correct on re-render
                    RefreshAvailableStock(model, tenantId);

                    LoadProducts();
                    return View(model);
                }
            }

            // Default fallback (if action is unknown)
            RefreshAvailableStock(model, tenantId);
            LoadProducts();
            return View(model);
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
            // Locks the specific product row for the duration of the current transaction.
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
