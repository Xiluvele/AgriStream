
using AgriStream.Web.Data;
using AgriStream.Web.ViewModels;
using Azure.Data.Tables;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
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

            // ==================================================
            // GET IRRIGATION ACTIVITY FROM AZURE TABLE STORAGE
            // ==================================================

            var irrigationActivities =   new List<IrrigationActivityViewModel>();


            try
            {
                var connectionString = _configuration["AzureStorage:ConnectionString"];

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    var tableClient = new TableClient(
                        connectionString,
                        "IrrigationActivity");

                    // Create the table if it does not already exist.
                    await tableClient.CreateIfNotExistsAsync();

                    // Get only devices the current user may view.
                    var allowedDeviceIds = await devicesQuery
                        .Select(d => d.Id)
                        .ToHashSetAsync();

                    // Query each authorized device partition.
                    foreach (var deviceId in allowedDeviceIds)
                    {
                        string partitionKey = $"device-{deviceId}";

                        await foreach (var entity in
                            tableClient.QueryAsync<TableEntity>(
                                e => e.PartitionKey == partitionKey))
                        {
                            var command = entity.GetString("Command");

                            var status = entity.GetString("Status");

                            var requestedAt =
                                entity.GetDateTimeOffset("RequestedAt");

                            irrigationActivities.Add(
                                new IrrigationActivityViewModel
                                {
                                    DeviceId = deviceId,

                                    Command = command ?? "",

                                    Status = status ?? "",

                                    RequestedAt =
                                        requestedAt ?? DateTimeOffset.MinValue
                                });
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to retrieve irrigation activity.");

                ViewBag.IrrigationError =
                    "Unable to load irrigation activity.";
            }

            // Display the five most recent commands.
            model.IrrigationActivities = irrigationActivities
                .OrderByDescending(a => a.RequestedAt)
                .Take(5)
                .ToList();

            return View(model);
        }
    }
}
