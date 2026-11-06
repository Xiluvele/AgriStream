
using AgriStream.Web.Data;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer")]
    public class IrrigationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly QueueClient _queue;

        public IrrigationController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users,
            IConfiguration configuration)
        {
            _db = db;
            _users = users;

            var connectionString =
                configuration["AzureStorage:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
            }

            _queue = new QueueClient(
                connectionString,
                "irrigation-commands",
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });
        }

        // DISPLAY IRRIGATION PAGE
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _users.GetUserId(User);

            var devices = _db.Devices
                .Include(d => d.Farm)
                .AsNoTracking()
                .AsQueryable();

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

            return View();
        }

        // SEND IRRIGATION COMMAND TO AZURE QUEUE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCommand(
            int deviceId,
            string command)
        {
            if (command != "Start" && command != "Stop")
            {
                TempData["Error"] =
                    "Invalid irrigation command.";

                return RedirectToAction(nameof(Index));
            }

            var userId = _users.GetUserId(User);

            bool allowed = await _db.Devices
                .AnyAsync(d =>
                    d.Id == deviceId &&
                    d.Farm != null &&
                    (User.IsInRole("Admin") ||
                     d.Farm.OwnerId == userId));

            if (!allowed)
                return Forbid();

            var irrigationCommand = new
            {
                CommandId = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                Command = command,
                RequestedBy = userId,
                RequestedAt = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(
                irrigationCommand);

            await _queue.CreateIfNotExistsAsync();

            await _queue.SendMessageAsync(json);

            TempData["Success"] =
                $"Irrigation command '{command}' queued successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
