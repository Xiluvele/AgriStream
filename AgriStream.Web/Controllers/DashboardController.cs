
using AgriStream.Web.Data;
using AgriStream.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer,Agronomist")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);

            bool isAdmin = User.IsInRole("Admin");

            string userId = user.Id;

            // Admin sees all farms.
            // Other users see farms they own.
            var farmsQuery = _context.Farms
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                farmsQuery = farmsQuery
                    .Where(f => f.OwnerId == userId);
            }

            // Devices are filtered through their farm owner.
            var devicesQuery = _context.Devices
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                devicesQuery = devicesQuery
                    .Where(d =>
                        d.Farm != null &&
                        d.Farm.OwnerId == userId);
            }

            var model = new DashboardViewModel
            {
                CurrentUserName = user.Email ?? "User",

                CurrentRole = roles.FirstOrDefault() ?? "Farmer",

                TotalFarms = await farmsQuery.CountAsync(),

                TotalDevices = await devicesQuery.CountAsync(),

                ActiveDevices = await devicesQuery
                    .CountAsync(d =>
                        d.OperationalStatus == "Active"),

                TotalUsers = isAdmin
                    ? await _context.Users.CountAsync()
                    : 0,

                RecentDevices = await devicesQuery
                    .OrderByDescending(d => d.LastUpdated)
                    .Take(5)
                    .Select(d => new DashboardDeviceViewModel
                    {
                        Id = d.Id,

                        Name = d.DeviceName,

                        DeviceType = d.DeviceType,

                        FarmName = d.Farm != null
                            ? d.Farm.FarmName
                            : "Not Assigned",

                        Status = d.OperationalStatus,

                        LastUpdated = d.LastUpdated
                    })
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
