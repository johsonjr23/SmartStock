using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.Services;
using TenantEntity = SmartStock.Models.Tenant;

namespace SmartStock.Areas.SystemAdmin.Controllers
{
    [Area("SystemAdmin")]
    [Authorize(Roles = "SystemAdmin")]
    public class RequestsController : Controller
    {
        private readonly SmartStockDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public RequestsController(SmartStockDbContext db, UserManager<ApplicationUser> userManager, IEmailService email)
        {
            _db = db;
            _userManager = userManager;
            _email = email;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var query = _db.TenantRequests.AsQueryable();
            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(r => r.Status == status);

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.StatusFilter = status ?? "All";
            ViewBag.PendingCount = await _db.TenantRequests.CountAsync(r => r.Status == "Pending");
            return View(requests);
        }

        public async Task<IActionResult> Details(int id)
        {
            var req = await _db.TenantRequests.FindAsync(id);
            if (req == null) return NotFound();
            return View(req);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string adminPassword, string? adminNotes)
        {
            var req = await _db.TenantRequests.FindAsync(id);
            if (req == null) return NotFound();

            if (req.Status != "Pending")
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                TempData["Error"] = "A password is required to create the tenant account.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (await _userManager.FindByEmailAsync(req.Email) != null)
            {
                TempData["Error"] = $"Email '{req.Email}' is already registered in the system.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var tenant = new TenantEntity
                {
                    Name      = req.ShopName.Trim(),
                    OwnerName = req.OwnerName.Trim(),
                    Phone     = req.Phone?.Trim(),
                    Email     = req.Email.Trim(),
                    IsActive  = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    UserName       = req.Email.Trim(),
                    Email          = req.Email.Trim(),
                    FullName       = req.OwnerName.Trim(),
                    TenantId       = tenant.Id,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, adminPassword);
                if (!result.Succeeded)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _userManager.AddToRoleAsync(user, "TenantAdmin");

                req.Status      = "Approved";
                req.ProcessedAt = DateTime.UtcNow;
                req.AdminNotes  = adminNotes?.Trim();
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                // Send credentials email — non-fatal if it fails
                try
                {
                    await _email.SendApprovalEmailAsync(req.Email, req.OwnerName, req.ShopName, adminPassword);
                    TempData["Success"] = $"Approved! Tenant '{tenant.Name}' created and credentials emailed to {req.Email}.";
                }
                catch (Exception emailEx)
                {
                    TempData["Success"] = $"Approved! Tenant '{tenant.Name}' created. However, the email could not be sent ({emailEx.Message}). Share the password manually.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? adminNotes)
        {
            var req = await _db.TenantRequests.FindAsync(id);
            if (req == null) return NotFound();

            if (req.Status != "Pending")
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            req.Status      = "Rejected";
            req.ProcessedAt = DateTime.UtcNow;
            req.AdminNotes  = adminNotes?.Trim();
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Request from '{req.ShopName}' has been rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}
