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
                        TotalAmount = 0m
                        // NOTE: Your current Sale entity (per compile errors) does not have SaleDate/PaymentType.
                        // We will not set them here.
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

                    // STEP 4: redirect to receipt/details
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

            // NOTE: Your Sale entity does not have SaleDate/PaymentType (compile errors).
            // We use CreatedAt as the receipt date, and a safe placeholder for payment.
            var vm = new SaleDetailsViewModel
            {
                Id = sale.Id,
                DisplayInvoiceNumber = !string.IsNullOrWhiteSpace(sale.InvoiceNumber)
                    ? sale.InvoiceNumber!
                    : $"SALE-{sale.Id:D6}",

                SaleDate = sale.CreatedAt,   // fallback date
                PaymentType = "N/A",         // until you add/store payment type in Sale
                TotalAmount = sale.TotalAmount,

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
