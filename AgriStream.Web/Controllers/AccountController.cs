using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
