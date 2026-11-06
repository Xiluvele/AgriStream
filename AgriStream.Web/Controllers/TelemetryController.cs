
using AgriStream.Web.Data;
using AgriStream.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer")]
    public class TelemetryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SensorTelemetryService _telemetryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TelemetryController(
            ApplicationDbContext context,
            SensorTelemetryService telemetryService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _telemetryService = telemetryService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? deviceId)
        {
            var userId = _userManager.GetUserId(User);

            var devices = _context.Devices
                .Include(d => d.Farm)
                .AsNoTracking()
                .AsQueryable();

            // Farmers can only access devices on their own farms.
            if (!User.IsInRole("Admin"))
            {
                devices = devices.Where(d =>
                    d.Farm != null &&
                    d.Farm.OwnerId == userId);
            }

            ViewBag.Devices = await devices
                .OrderBy(d => d.DeviceName)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.DeviceName
                })
                .ToListAsync();

            ViewBag.SelectedDeviceId = deviceId;

            if (deviceId.HasValue)
            {
                bool allowed = await devices.AnyAsync(
                    d => d.Id == deviceId.Value);

                if (!allowed)
                    return Forbid();

                try
                {
                    ViewBag.Readings =
                        await _telemetryService.GetReadingsAsync(
                            deviceId.Value);
                }
                catch (HttpRequestException)
                {
                    ViewBag.Error =
                        "Unable to connect to Azure Functions.";
                }
                catch (Exception)
                {
                    ViewBag.Error =
                        "Unable to retrieve sensor readings.";
                }
            }

            return View();
        }
    }
}
