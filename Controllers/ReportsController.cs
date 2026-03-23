using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ReportsLocalization;
using SmartStock.ViewModels;
using SmartStock.ViewModels.Reports;



namespace SmartStock.Controllers
{
    [Authorize(Roles = "TenantAdmin")]
    public class ReportsController : BaseController
    {
        private readonly SmartStockDbContext _context;

        public ReportsController(
            SmartStockDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(userManager)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> InventoryValuation(int? categoryId, bool includeZero = false, string? q = null, string? lang = "en")
        {
            var tenantId = GetTenantId();
            var isSw = string.Equals(lang, "sw", StringComparison.OrdinalIgnoreCase);

            var vm = new InventoryValuationReportVm
            {
                CategoryId = categoryId,
                IncludeZero = includeZero,
                Query = q,
                Lang = (lang ?? "en").ToLower(),
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Labels = new InventoryValuationLabelsVm
                {
                    Title = isSw ? "Thamani ya Hisa (Stock)" : "Inventory Valuation",
                    Subtitle = isSw ? "Thamani ya bidhaa zilizopo dukani kwa gharama ya wastani" : "Current stock value based on average cost",

                    Category = isSw ? "Kundi" : "Category",
                    Search = isSw ? "Tafuta" : "Search",
                    IncludeZero = isSw ? "Onyesha hata zenye sifuri" : "Include zero stock",
                    Apply = isSw ? "Tumia" : "Apply",
                    Reset = isSw ? "Anza upya" : "Reset",

                    ColSku = "SKU",
                    ColProduct = isSw ? "Bidhaa" : "Product",
                    ColCategory = isSw ? "Kundi" : "Category",
                    ColStockQty = isSw ? "Idadi" : "Stock Qty",
                    ColAvgCost = isSw ? "Gharama ya Wastani" : "Average Cost",
                    ColStockValue = isSw ? "Thamani ya Bidhaa" : "Stock Value",

                    TotalUnits = isSw ? "Jumla ya Idadi" : "Total Units",
                    TotalValue = isSw ? "Jumla ya Thamani" : "Total Stock Value",
                    NoItems = isSw ? "Hakuna bidhaa." : "No items found.",
                    AllCategories = isSw ? "-- Zote --" : "-- All --",
                    GeneratedAt = isSw ? "Imetengenezwa" : "Generated"
                }
            };

            // Category dropdown (tenant-scoped)
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.CategoryOptions = new SelectList(categories, "Id", "Name", categoryId);

            // Base products
            var products = _context.Products
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                products = products.Where(p => p.Name.Contains(term) || (p.SKU != null && p.SKU.Contains(term)));
            }



            // =========================
            // STOCK – LEDGER BASED (InventoryTransactions)
            // =========================

            var stockAgg = _context.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId)
                .GroupBy(t => t.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.QuantityChange)
                });

            var rowsQuery =
                from p in products

                join st in stockAgg on p.Id equals st.ProductId into stj
                from st in stj.DefaultIfEmpty()

                let stockQty = (decimal?)st.Qty ?? 0m

                select new InventoryValuationRowVm
                {
                    ProductId = p.Id,
                    SKU = p.SKU,
                    Name = p.Name,
                    Category = p.Category!.Name,

                    StockQty = stockQty,
                    AvgCost = p.BuyingPrice,
                    InventoryValue = stockQty * p.BuyingPrice
                };

            if (!includeZero)
                rowsQuery = rowsQuery.Where(r => r.StockQty != 0);

            vm.Items = await rowsQuery.OrderBy(r => r.Name).ToListAsync();

            vm.TotalStockUnits = vm.Items.Sum(x => x.StockQty);
            vm.TotalInventoryValue = vm.Items.Sum(x => x.InventoryValue);

            return View(vm);
        }





        // GET: /Reports/Profit
        [HttpGet]
        public async Task<IActionResult> Profit(DateTime? from = null, DateTime? to = null, string? lang = "en")
        {
            var tenantId = GetTenantId();

            var start = (from ?? DateTime.Today).Date;
            var end = (to ?? DateTime.Today).Date;

            if (end < start)
            {
                var tmp = start;
                start = end;
                end = tmp;
            }

            var endExclusive = end.AddDays(1);

            // =========================
            // EXPENSES (date-only)
            // =========================
            var expensesQuery = _context.Expenses
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end);

            var totalExpenses =
                await expensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var expensesByType = await expensesQuery
                .GroupBy(e => e.Type)
                .Select(g => new ExpenseByTypeRow
                {
                    Type = g.Key,
                    Amount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            // =========================
            // SALES (ORIGINAL transactions)
            // =========================
            var grossSalesAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Where(s => s.OriginalSaleId == null)          // ORIGINAL sales
                .Where(s => s.Status != SaleStatus.Voided)    // exclude voids
                .Where(s => s.CompletedAt != null)
                .Where(s => s.CompletedAt >= start && s.CompletedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            // =========================
            // REFUNDS (REFUND transactions only)
            // =========================
            var rawRefundAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Where(s => s.OriginalSaleId != null)          // REFUND rows
                .Where(s => s.Status == SaleStatus.Refunded)
                .Where(s => s.RefundedAt != null)
                .Where(s => s.RefundedAt >= start && s.RefundedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var refundAmount = rawRefundAmount < 0
                ? -rawRefundAmount
                : rawRefundAmount;

            var netSalesAmount = grossSalesAmount - refundAmount;

            // =========================
            // COGS – ORIGINAL SALES
            // Phase 2: use BuyingPriceAtSale snapshot; fallback to Product.BuyingPrice for old rows
            // =========================
            var salesCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.OriginalSaleId == null)
                .Where(si => si.Sale.Status != SaleStatus.Voided)
                .Where(si => si.Sale.CompletedAt != null)
                .Where(si => si.Sale.CompletedAt >= start && si.Sale.CompletedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * (si.BuyingPriceAtSale ?? si.Product.BuyingPrice)) ?? 0m;

            // =========================
            // COGS – REFUND TRANSACTIONS
            // Phase 2: use BuyingPriceAtSale snapshot; fallback to Product.BuyingPrice for old rows
            // =========================
            var refundCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.OriginalSaleId != null)
                .Where(si => si.Sale.Status == SaleStatus.Refunded)
                .Where(si => si.Sale.RefundedAt != null)
                .Where(si => si.Sale.RefundedAt >= start && si.Sale.RefundedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * (si.BuyingPriceAtSale ?? si.Product.BuyingPrice)) ?? 0m;

            var netCOGS = salesCOGS - refundCOGS;

            // =========================
            // PROFIT
            // =========================
            var grossProfit = netSalesAmount - netCOGS;
            var netProfit = grossProfit - totalExpenses;

            var vm = new ProfitReportViewModel
            {
                From = start,
                To = end,

                Lang = (lang ?? "en").ToLower(),
                Labels = ReportLabelProvider.Get(lang),

                TotalExpenses = totalExpenses,
                ExpensesByType = expensesByType,

                GrossSalesAmount = grossSalesAmount,
                RefundAmount = refundAmount,
                NetSalesAmount = netSalesAmount,

                SalesCOGS = salesCOGS,
                RefundCOGS = refundCOGS,
                NetCOGS = netCOGS,

                GrossProfit = grossProfit,
                NetProfit = netProfit
            };

            return View(vm);
        }
        public async Task<IActionResult> ProductMargin(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = GetTenantId();

            // Default date range (optional but recommended)
            var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var query = await _context.SaleItems
    .Where(si =>
        si.Sale.TenantId == tenantId &&
        si.Sale.Status == SaleStatus.Completed &&
        si.Sale.CompletedAt >= from &&
        si.Sale.CompletedAt <= to)
    .GroupBy(si => new { si.ProductId, si.Product.Name })
    .Select(g => new ProductMarginRowViewModel
    {
        ProductName = g.Key.Name,

        TotalQuantity = g.Sum(x => x.Quantity),

        Revenue = g.Sum(x =>
            x.Quantity * x.SellingPrice),

        Cost = g.Sum(x =>
            x.Quantity * x.BuyingPriceAtSale.GetValueOrDefault()),

        Profit = g.Sum(x =>
            (x.Quantity * x.SellingPrice) -
            (x.Quantity * x.BuyingPriceAtSale.GetValueOrDefault()))
    })
    .OrderByDescending(x => x.Profit)
    .ToListAsync();
            // Calculate margin percentage after materialization
            foreach (var item in query)
            {
                item.MarginPercentage = item.Revenue == 0
                    ? 0
                    : (item.Profit / item.Revenue) * 100;
            }

            var viewModel = new ProductMarginReportViewModel
            {
                FromDate = from,
                ToDate = to,
                Items = query
            };

            return View(viewModel);
        }

    }
}
