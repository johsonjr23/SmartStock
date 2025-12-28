using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize]
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

        // GET: Sales/Create
        public IActionResult Create()
        {
            var tenantId = GetTenantId();

            ViewBag.Products = new SelectList(
                _context.Products
                    .Where(p => p.TenantId == tenantId)
                    .OrderBy(p => p.Name)
                    .AsNoTracking(),
                "Id",
                "Name"
            );

            var vm = new CreateSaleViewModel();
            vm.Items.Add(new CreateSaleItemViewModel());

            return View(vm);
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSaleViewModel vm)
        {
            var tenantId = GetTenantId();

            if (!ModelState.IsValid)
            {
                ViewBag.Products = new SelectList(
                    _context.Products
                        .Where(p => p.TenantId == tenantId)
                        .OrderBy(p => p.Name),
                    "Id",
                    "Name"
                );

                return View(vm);
            }

            // 1) Create Sale
            var sale = new Sale
            {
                TenantId = tenantId,
                SaleDate = vm.SaleDate,
                PaymentType = vm.PaymentType,
                TotalAmount = 0m
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            decimal total = 0m;

            // 2) Create SaleItems + Stock OUT
            foreach (var item in vm.Items)
            {
                if (item.Quantity <= 0)
                    continue;

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.TenantId == tenantId);

                if (product == null)
                    continue;

                var subTotal = item.Quantity * item.SellingPrice;

                var saleItem = new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SellingPrice = item.SellingPrice,
                    SubTotal = subTotal
                };


                _context.SaleItems.Add(saleItem);
                total += subTotal;

                var stockOut = new InventoryTransaction
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    QuantityChange = -item.Quantity,
                    TransactionType = InventoryTransactionType.Sale,
                    SaleId = sale.Id
                };

                _context.InventoryTransactions.Add(stockOut);
            }

            // 3) Update totals
            sale.TotalAmount = total;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Sales");
        }
        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var tenantId = GetTenantId();

            var sales = await _context.Sales
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

    }
}
