using Microsoft.AspNetCore.Mvc;

namespace SmartStock.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Tenant Dashboard";
            return View();
        }
    }
}
