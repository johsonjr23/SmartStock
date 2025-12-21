using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using Microsoft.AspNetCore.Mvc.Rendering; // add at top with other usings

namespace SmartStock.Controllers
{
    public class ProductsController : Controller
    {
        private readonly SmartStockDbContext _context;

        public ProductsController(SmartStockDbContext context)
        {
            _context = context;
        }

        // LIST PRODUCTS
        public async Task<IActionResult> Index()
        {
            // Later we will filter by TenantId
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .ToListAsync();
            var stockDict = new Dictionary<int, int>();

            foreach (var product in products)
            {
                stockDict[product.Id] = GetCurrentStock(product.Id);
            }

            ViewBag.Stock = stockDict;

            return View(products);
        }

        // SHOW CREATE FORM
        // GET: Products/Create
        public IActionResult Create()
        {
            // populate dropdowns
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["Units"] = new SelectList(_context.Units, "Id", "Name");
            return View();
        }

        // SAVE PRODUCT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // REMOVE Tenant validation (we set it manually)
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
            {
                ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                ViewData["Units"] = new SelectList(_context.Units, "Id", "Name", product.UnitId);
                return View(product);
            }

            // SET tenant manually (later from logged-in user)
            product.TenantId = 1;
            product.CreatedAt = DateTime.Now;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // SHOW EDIT FORM
        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["Units"] = new SelectList(_context.Units, "Id", "Name", product.UnitId);
            return View(product);
        }

        // SAVE EDITED PRODUCT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(product);

            _context.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // SHOW DELETE CONFIRMATION
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // PERFORM DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.SaleItems)
                .Include(p => p.PurchaseItems)
                .Include(p => p.StockHistories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // BLOCK delete if product is used
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

        // SHOW PRODUCT DETAILS
        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }




        [HttpPost]
        public IActionResult StockIn(int productId, int quantity)
        {
            if (quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                return NotFound();

            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                QuantityChange = quantity, // + stock
                TransactionType = InventoryTransactionType.Purchase,
                TenantId = product.TenantId
            };

            _context.InventoryTransactions.Add(transaction);
            _context.SaveChanges();

            return Ok("Stock added successfully.");
        }

        private int GetCurrentStock(int productId)
        {
            return _context.InventoryTransactions
                .Where(t => t.ProductId == productId)
                .Sum(t => t.QuantityChange);
        }

        public IActionResult StockIn(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound();

            return View(product);
        }



    }
}
