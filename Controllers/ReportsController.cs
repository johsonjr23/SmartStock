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

        // ─── helper ──────────────────────────────────────────────────────────
        private async Task<string> GetTenantNameAsync(int tenantId)
        {
            var t = await _context.Tenants.FindAsync(tenantId);
            return t?.Name ?? "SmartStock";
        }

        // ─── Profit ──────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Profit(DateTime? from = null, DateTime? to = null, string? lang = "en")
        {
            var tenantId = GetTenantId();

            var start = (from ?? DateTime.Today).Date;
            var end   = (to   ?? DateTime.Today).Date;

            if (end < start) (start, end) = (end, start);

            var endExclusive = end.AddDays(1);

            var expensesQuery = _context.Expenses
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end);

            var totalExpenses = await expensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var expensesByType = await expensesQuery
                .GroupBy(e => e.Type)
                .Select(g => new ExpenseByTypeRow { Type = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var grossSalesAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.OriginalSaleId == null
                         && s.Status != SaleStatus.Voided
                         && s.CompletedAt != null
                         && s.CompletedAt >= start && s.CompletedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var rawRefundAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.OriginalSaleId != null
                         && s.Status == SaleStatus.Refunded
                         && s.RefundedAt != null
                         && s.RefundedAt >= start && s.RefundedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var refundAmount  = rawRefundAmount < 0 ? -rawRefundAmount : rawRefundAmount;
            var netSalesAmount = grossSalesAmount - refundAmount;

            var salesCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId
                          && si.Sale.OriginalSaleId == null
                          && si.Sale.Status != SaleStatus.Voided
                          && si.Sale.CompletedAt != null
                          && si.Sale.CompletedAt >= start && si.Sale.CompletedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * (si.BuyingPriceAtSale ?? si.Product.BuyingPrice)) ?? 0m;

            var refundCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId
                          && si.Sale.OriginalSaleId != null
                          && si.Sale.Status == SaleStatus.Refunded
                          && si.Sale.RefundedAt != null
                          && si.Sale.RefundedAt >= start && si.Sale.RefundedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * (si.BuyingPriceAtSale ?? si.Product.BuyingPrice)) ?? 0m;

            var netCOGS     = salesCOGS - refundCOGS;
            var grossProfit = netSalesAmount - netCOGS;
            var netProfit   = grossProfit - totalExpenses;

            var vm = new ProfitReportViewModel
            {
                From            = start,
                To              = end,
                TenantName      = await GetTenantNameAsync(tenantId),
                Lang            = (lang ?? "en").ToLower(),
                Labels          = ReportLabelProvider.Get(lang),
                TotalExpenses   = totalExpenses,
                ExpensesByType  = expensesByType,
                GrossSalesAmount = grossSalesAmount,
                RefundAmount    = refundAmount,
                NetSalesAmount  = netSalesAmount,
                SalesCOGS       = salesCOGS,
                RefundCOGS      = refundCOGS,
                NetCOGS         = netCOGS,
                GrossProfit     = grossProfit,
                NetProfit       = netProfit
            };

            return View(vm);
        }

        // ─── Inventory Valuation ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> InventoryValuation(int? categoryId, bool includeZero = false, string? q = null, string? lang = "en")
        {
            var tenantId = GetTenantId();
            var isSw = string.Equals(lang, "sw", StringComparison.OrdinalIgnoreCase);

            var vm = new InventoryValuationReportVm
            {
                CategoryId       = categoryId,
                IncludeZero      = includeZero,
                Query            = q,
                Lang             = (lang ?? "en").ToLower(),
                TenantName       = await GetTenantNameAsync(tenantId),
                GeneratedAtUtc   = DateTimeOffset.UtcNow,
                Labels = new InventoryValuationLabelsVm
                {
                    Title        = isSw ? "Thamani ya Hisa (Stock)" : "Inventory Valuation",
                    Subtitle     = isSw ? "Thamani ya bidhaa zilizopo dukani kwa gharama ya wastani" : "Current stock value based on average cost",
                    Category     = isSw ? "Kundi" : "Category",
                    Search       = isSw ? "Tafuta" : "Search",
                    IncludeZero  = isSw ? "Onyesha hata zenye sifuri" : "Include zero stock",
                    Apply        = isSw ? "Tumia" : "Apply",
                    Reset        = isSw ? "Anza upya" : "Reset",
                    ColSku       = "SKU",
                    ColProduct   = isSw ? "Bidhaa" : "Product",
                    ColCategory  = isSw ? "Kundi" : "Category",
                    ColStockQty  = isSw ? "Idadi" : "Stock Qty",
                    ColAvgCost   = isSw ? "Gharama ya Wastani" : "Average Cost",
                    ColStockValue = isSw ? "Thamani ya Bidhaa" : "Stock Value",
                    TotalUnits   = isSw ? "Jumla ya Idadi" : "Total Units",
                    TotalValue   = isSw ? "Jumla ya Thamani" : "Total Stock Value",
                    NoItems      = isSw ? "Hakuna bidhaa." : "No items found.",
                    AllCategories = isSw ? "-- Zote --" : "-- All --",
                    GeneratedAt  = isSw ? "Imetengenezwa" : "Generated"
                }
            };

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.CategoryOptions = new SelectList(categories, "Id", "Name", categoryId);

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

            var stockAgg = _context.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId)
                .GroupBy(t => t.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.QuantityChange) });

            var rowsQuery =
                from p in products
                join st in stockAgg on p.Id equals st.ProductId into stj
                from st in stj.DefaultIfEmpty()
                let stockQty = (decimal?)st.Qty ?? 0m
                select new InventoryValuationRowVm
                {
                    ProductId      = p.Id,
                    SKU            = p.SKU,
                    Name           = p.Name,
                    Category       = p.Category!.Name,
                    StockQty       = stockQty,
                    AvgCost        = p.BuyingPrice,
                    InventoryValue = stockQty * p.BuyingPrice
                };

            if (!includeZero)
                rowsQuery = rowsQuery.Where(r => r.StockQty != 0);

            vm.Items               = await rowsQuery.OrderBy(r => r.Name).ToListAsync();
            vm.TotalStockUnits     = vm.Items.Sum(x => x.StockQty);
            vm.TotalInventoryValue = vm.Items.Sum(x => x.InventoryValue);

            return View(vm);
        }

        // ─── Product Margin ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ProductMargin(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = GetTenantId();
            var from = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to   = (toDate   ?? DateTime.Today).Date;
            if (to < from) (from, to) = (to, from);

            var toExclusive = to.AddDays(1);

            var rows = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.Sale.TenantId == tenantId
                          && si.Sale.Status == SaleStatus.Completed
                          && si.Sale.CompletedAt >= from
                          && si.Sale.CompletedAt < toExclusive)
                .GroupBy(si => new { si.ProductId, si.Product.Name })
                .Select(g => new ProductMarginRowViewModel
                {
                    ProductName   = g.Key.Name,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    Revenue       = g.Sum(x => x.Quantity * x.SellingPrice),
                    Cost          = g.Sum(x => x.Quantity * (x.BuyingPriceAtSale ?? x.Product.BuyingPrice)),
                    Profit        = g.Sum(x => x.Quantity * x.SellingPrice
                                             - x.Quantity * (x.BuyingPriceAtSale ?? x.Product.BuyingPrice))
                })
                .OrderByDescending(x => x.Profit)
                .ToListAsync();

            foreach (var item in rows)
                item.MarginPercentage = item.Revenue == 0 ? 0 : item.Profit / item.Revenue * 100;

            return View(new ProductMarginReportViewModel
            {
                FromDate   = from,
                ToDate     = to,
                TenantName = await GetTenantNameAsync(tenantId),
                Items      = rows
            });
        }

        // ─── Daily Sales ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DailySales(DateTime? from = null, DateTime? to = null)
        {
            var tenantId = GetTenantId();
            var start = (from ?? DateTime.Today.AddDays(-29)).Date;
            var end   = (to   ?? DateTime.Today).Date;
            if (end < start) (start, end) = (end, start);

            var endExclusive = end.AddDays(1);

            var raw = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId
                         && s.OriginalSaleId == null
                         && s.Status == SaleStatus.Completed
                         && s.CompletedAt != null
                         && s.CompletedAt >= start && s.CompletedAt < endExclusive)
                .GroupBy(s => s.CompletedAt!.Value.Date)
                .Select(g => new DailySalesRowVm
                {
                    Date         = g.Key,
                    Transactions = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return View(new DailySalesReportViewModel
            {
                From             = start,
                To               = end,
                TenantName       = await GetTenantNameAsync(tenantId),
                GeneratedAtUtc   = DateTimeOffset.UtcNow,
                Rows             = raw
            });
        }

        // ─── Low Stock ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var tenantId = GetTenantId();

            var stockAgg = _context.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId)
                .GroupBy(t => t.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.QuantityChange) });

            var items = await (
                from p in _context.Products
                    .AsNoTracking()
                    .Where(p => p.TenantId == tenantId && p.IsActive)
                    .Include(p => p.Category)
                join st in stockAgg on p.Id equals st.ProductId into stj
                from st in stj.DefaultIfEmpty()
                let qty = (decimal?)st.Qty ?? 0m
                where qty <= (p.ReorderLevel != null ? (decimal)p.ReorderLevel : 0m)
                select new LowStockRowVm
                {
                    ProductId    = p.Id,
                    SKU          = p.SKU,
                    Name         = p.Name,
                    Category     = p.Category != null ? p.Category.Name : "",
                    CurrentStock = qty,
                    ReorderLevel = p.ReorderLevel
                })
                .OrderBy(r => r.CurrentStock)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return View(new LowStockReportViewModel
            {
                TenantName     = await GetTenantNameAsync(tenantId),
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Items          = items
            });
        }

        // ─── Void / Refund Log ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VoidRefundLog(DateTime? from = null, DateTime? to = null)
        {
            var tenantId = GetTenantId();
            var start = (from ?? DateTime.Today.AddDays(-29)).Date;
            var end   = (to   ?? DateTime.Today).Date;
            if (end < start) (start, end) = (end, start);

            var endExclusive = end.AddDays(1);

            var voided = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId
                         && s.Status == SaleStatus.Voided
                         && s.VoidedAt != null
                         && s.VoidedAt >= start && s.VoidedAt < endExclusive)
                .Select(s => new VoidRefundRowVm
                {
                    SaleId        = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    Type          = "Voided",
                    EventDate     = s.VoidedAt!.Value,
                    Amount        = s.TotalAmount,
                    Reason        = s.VoidReason
                })
                .ToListAsync();

            var refunded = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId
                         && s.Status == SaleStatus.Refunded
                         && s.OriginalSaleId != null
                         && s.RefundedAt != null
                         && s.RefundedAt >= start && s.RefundedAt < endExclusive)
                .Select(s => new VoidRefundRowVm
                {
                    SaleId        = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    Type          = "Refunded",
                    EventDate     = s.RefundedAt!.Value,
                    Amount        = s.TotalAmount < 0 ? -s.TotalAmount : s.TotalAmount,
                    Reason        = s.RefundReason
                })
                .ToListAsync();

            var rows = voided.Concat(refunded).OrderByDescending(r => r.EventDate).ToList();

            return View(new VoidRefundLogViewModel
            {
                From           = start,
                To             = end,
                TenantName     = await GetTenantNameAsync(tenantId),
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Rows           = rows
            });
        }
    }
}
