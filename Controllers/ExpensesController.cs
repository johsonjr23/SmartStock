using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    public class ExpensesController : BaseController
    {
        private readonly SmartStockDbContext _context;

        public ExpensesController(
            SmartStockDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(userManager)
        {
            _context = context;
        }

        // =========================
        // GET: /Expenses
        // =========================
        public async Task<IActionResult> Index(
            DateTime? from = null,
            DateTime? to = null,
            string? q = null
        )
        {
            var tenantId = GetTenantId();

            var query = _context.Expenses
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId);

            if (from.HasValue)
                query = query.Where(e => e.ExpenseDate >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(e => e.ExpenseDate <= to.Value.Date);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(e =>
                    e.Type.Contains(q) ||
                    (e.Payee != null && e.Payee.Contains(q)) ||
                    (e.Notes != null && e.Notes.Contains(q)));
            }

            var expenses = await query
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.Id)
                .ToListAsync();

            return View(expenses);
        }

        // =========================
        // GET: /Expenses/New
        // =========================
        [HttpGet]
        public IActionResult New()
        {
            return View(new CreateExpenseViewModel());
        }

        // =========================
        // POST: /Expenses/New
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(CreateExpenseViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var tenantId = GetTenantId();

            var expense = new Expense
            {
                TenantId = tenantId,
                Type = model.Type.Trim(),
                Amount = model.Amount,
                Payee = string.IsNullOrWhiteSpace(model.Payee)
                    ? null
                    : model.Payee.Trim(),
                Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? null
                    : model.Notes.Trim(),
                ExpenseDate = model.ExpenseDate.Date,
                CreatedAt = DateTime.UtcNow
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Locked after save → Details only
            return RedirectToAction(nameof(Details), new { id = expense.Id });
        }

        // =========================
        // GET: /Expenses/Details/5
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var tenantId = GetTenantId();

            var expense = await _context.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.TenantId == tenantId);

            if (expense == null)
                return NotFound();

            return View(expense);
        }

        // ❌ NO Edit
        // ❌ NO Delete
        // Expenses are immutable by design (Option B)
    }
}
