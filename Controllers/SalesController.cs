using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly SmartStockDbContext _context;

        public SalesController(SmartStockDbContext context)
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

            // Start with one empty row
            vm.Items.Add(new CreateSaleItemViewModel());

            return View(vm);
        }

        // =========================
        // Helpers
        // =========================
        private int GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId");

            if (tenantClaim == null)
            {
                throw new Exception(
                    "TenantId claim not found. Ensure the user is authenticated and TenantId is added to claims."
                );
            }

            return int.Parse(tenantClaim.Value);
        }
    }
}
