using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

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
        public async Task<IActionResult> Profit(DateTime? from = null, DateTime? to = null)
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

            // IMPORTANT: endExclusive makes the range include the full end day
            var endExclusive = end.AddDays(1);

            // =========================
            // EXPENSES (ExpenseDate is date-only)
            // =========================
            var expensesQuery = _context.Expenses
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end);

            var totalExpenses = await expensesQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;

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
            // SALES (CompletedAt is datetime)
            // =========================
            var grossSalesAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Where(s => s.Status == SaleStatus.Completed)
                .Where(s => s.CompletedAt != null)
                .Where(s => s.CompletedAt >= start && s.CompletedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            // =========================
            // REFUNDS (RefundedAt is datetime)
            // =========================
            var rawRefundAmount = await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Where(s => s.Status == SaleStatus.Refunded)
                .Where(s => s.RefundedAt != null)
                .Where(s => s.RefundedAt >= start && s.RefundedAt < endExclusive)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var refundAmount = rawRefundAmount < 0 ? -rawRefundAmount : rawRefundAmount;

            var netSalesAmount = grossSalesAmount - refundAmount;

            // =========================
            // COGS (use Sale.CompletedAt/RefundedAt filters)
            // =========================
            var salesCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.Status == SaleStatus.Completed)
                .Where(si => si.Sale.CompletedAt != null)
                .Where(si => si.Sale.CompletedAt >= start && si.Sale.CompletedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * si.Product.BuyingPrice) ?? 0m;

            var refundCOGS = await _context.SaleItems
                .AsNoTracking()
                .Where(si => si.TenantId == tenantId)
                .Where(si => si.Sale.Status == SaleStatus.Refunded)
                .Where(si => si.Sale.RefundedAt != null)
                .Where(si => si.Sale.RefundedAt >= start && si.Sale.RefundedAt < endExclusive)
                .SumAsync(si => (decimal?)si.Quantity * si.Product.BuyingPrice) ?? 0m;

            var netCOGS = salesCOGS - refundCOGS;

            var grossProfit = netSalesAmount - netCOGS;
            var netProfit = grossProfit - totalExpenses;

            var vm = new ProfitReportViewModel
            {
                From = start,
                To = end,

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
