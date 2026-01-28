using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;
using SmartStock.ReportsLocalization;

namespace SmartStock.Controllers
{
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
            // =========================
            var salesCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.OriginalSaleId == null)
                .Where(si => si.Sale.Status != SaleStatus.Voided)
                .Where(si => si.Sale.CompletedAt != null)
                .Where(si => si.Sale.CompletedAt >= start && si.Sale.CompletedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * si.Product.BuyingPrice) ?? 0m;

            // =========================
            // COGS – REFUND TRANSACTIONS
            // =========================
            var refundCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.OriginalSaleId != null)
                .Where(si => si.Sale.Status == SaleStatus.Refunded)
                .Where(si => si.Sale.RefundedAt != null)
                .Where(si => si.Sale.RefundedAt >= start && si.Sale.RefundedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * si.Product.BuyingPrice) ?? 0m;

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
    }
}
