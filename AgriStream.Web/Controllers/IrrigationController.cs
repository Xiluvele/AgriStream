using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Web.Controllers
{
    public class IrrigationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
