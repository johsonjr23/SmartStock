using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    public class SalesController : Controller
    {
        private readonly SmartStockDbContext _context;

        public SalesController(SmartStockDbContext context)
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
        public IActionResult New(
            CreateSaleViewModel model,
            string action,
            int? removeIndex)
        {
            model.Items ??= new();

            // -------------------------
            // ADD PRODUCT
            // -------------------------
            if (action == "add")
            {
                if (string.IsNullOrWhiteSpace(model.ProductSearch))
                {
                    ModelState.AddModelError("", "Type a product name or SKU");
                    return View(model);
                }

                var search = model.ProductSearch.Trim();

                var product = _context.Products
                    .Where(p => p.IsActive)
                    .Where(p =>
                        p.Name.Contains(search) ||
                        (p.SKU != null && p.SKU.Contains(search)))
                    .OrderBy(p => p.Name)
                    .FirstOrDefault();

                if (product == null)
                {
                    ModelState.AddModelError("", "Product not found");
                    return View(model);
                }

                var existingItem = model.Items
                    .FirstOrDefault(i => i.ProductId == product.Id);

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
                        UnitPrice = product.SellingPrice,
                        Quantity = 1,
                        AvailableStock = GetCurrentStock(product.Id)
                    });
                }

                model.ProductSearch = "";
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
                return View(model);
            }

            // -------------------------
            // COMPLETE SALE (WITH STOCK VALIDATION)
            // -------------------------
            if (action == "complete")
            {
                if (model.Items.Count == 0)
                {
                    ModelState.AddModelError("", "Add at least one item");
                    return View(model);
                }

                // 🔒 STOCK VALIDATION
                foreach (var item in model.Items)
                {
                    var currentStock = GetCurrentStock(item.ProductId);

                    if (item.Quantity > currentStock)
                    {
                        ModelState.AddModelError("",
                            $"Not enough stock for {item.ProductName}. " +
                            $"Available: {currentStock}");
                        return View(model);
                    }
                }

                // 🚫 NO SAVE YET (STEP 6)
                TempData["Success"] = "Stock validated. Ready to save sale.";
                return RedirectToAction("New");
            }

            return View(model);
        }

        // =========================
        // AUTOCOMPLETE ENDPOINT
        // =========================
        [HttpGet]
        public IActionResult SearchProducts(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var results = _context.Products
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
            ViewBag.Products = _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToList();
        }

        private int GetCurrentStock(int productId)
        {
            return _context.InventoryTransactions
                .Where(t => t.ProductId == productId)
                .Sum(t => t.QuantityChange);
        }
    }
}
