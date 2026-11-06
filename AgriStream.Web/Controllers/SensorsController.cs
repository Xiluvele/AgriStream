using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Web.Controllers
{
    public class SensorsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
