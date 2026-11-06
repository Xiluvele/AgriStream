
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using AgriStream.Web.ViewModels.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer")]
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] AllowedStatuses =
        {
            "Active",
            "Inactive"
        };

        private static readonly string[] AllowedTypes =
        {
            "Soil Moisture Sensor",
            "Temperature Sensor",
            "Humidity Sensor",
            "Irrigation Controller",
            "Water Level Sensor",
            "Weather Station"
        };

        public DevicesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private IQueryable<Farm> AccessibleFarms()
        {
            var query = _context.Farms.AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);

                query = query.Where(f => f.OwnerId == userId);
            }

            return query;
        }

        private IQueryable<Device> AccessibleDevices()
        {
            var query = _context.Devices.AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);

                query = query.Where(d =>
                    d.Farm != null &&
                    d.Farm.OwnerId == userId);
            }

            return query;
        }

        private async Task LoadFarmOptionsAsync(
            DeviceFormViewModel model)
        {
            model.Farms = await AccessibleFarms()
                .AsNoTracking()
                .OrderBy(f => f.FarmName)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.FarmName
                })
                .ToListAsync();
        }

        // GET: Devices
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int page = 1)
        {
            const int pageSize = 10;

            page = Math.Max(1, page);

            var query = AccessibleDevices().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(d =>
                    d.DeviceName.Contains(search) ||
                    d.DeviceType.Contains(search) ||
                    (d.Farm != null &&
                     d.Farm.FarmName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                AllowedStatuses.Contains(status))
            {
                query = query.Where(d =>
                    d.OperationalStatus == status);
            }
            else
            {
                status = null;
            }

            int totalDevices = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalDevices / (double)pageSize);

            if (totalPages > 0)
            {
                page = Math.Min(page, totalPages);
            }

            var devices = await query
                .OrderByDescending(d => d.LastUpdated)
                .ThenBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeviceListItemViewModel
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    OperationalStatus = d.OperationalStatus,
                    FarmName = d.Farm != null
                        ? d.Farm.FarmName
                        : "Not Assigned",
                    LastUpdated = d.LastUpdated
                })
                .ToListAsync();

            var model = new DeviceIndexViewModel
            {
                Devices = devices,
                Search = search,
                Status = status,
                Page = page,
                PageSize = pageSize,
                TotalDevices = totalDevices
            };

            return View(model);
        }

        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var device = await AccessibleDevices()
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DeviceDetailsViewModel
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    OperationalStatus = d.OperationalStatus,
                    FarmId = d.FarmId,
                    FarmName = d.Farm != null
                        ? d.Farm.FarmName
                        : "Not Assigned",
                    LastUpdated = d.LastUpdated
                })
                .FirstOrDefaultAsync();

            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // GET: Devices/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new DeviceFormViewModel();

            await LoadFarmOptionsAsync(model);

            return View(model);
        }

        // POST: Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            DeviceFormViewModel model)
        {
            await ValidateFormAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadFarmOptionsAsync(model);
                return View(model);
            }

            var device = new Device
            {
                DeviceName = model.DeviceName.Trim(),
                DeviceType = model.DeviceType,
                OperationalStatus = model.OperationalStatus,
                FarmId = model.FarmId,
                LastUpdated = DateTime.UtcNow
            };

            _context.Devices.Add(device);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device registered successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Devices/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await AccessibleDevices()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                return NotFound();
            }

            var model = new DeviceFormViewModel
            {
                Id = device.Id,
                DeviceName = device.DeviceName,
                DeviceType = device.DeviceType,
                OperationalStatus = device.OperationalStatus,
                FarmId = device.FarmId
            };

            await LoadFarmOptionsAsync(model);

            return View(model);
        }

        // POST: Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            DeviceFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var device = await AccessibleDevices()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                return NotFound();
            }

            await ValidateFormAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadFarmOptionsAsync(model);
                return View(model);
            }

            device.DeviceName = model.DeviceName.Trim();
            device.DeviceType = model.DeviceType;
            device.OperationalStatus = model.OperationalStatus;
            device.FarmId = model.FarmId;
            device.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateFormAsync(
            DeviceFormViewModel model)
        {
            if (!AllowedTypes.Contains(model.DeviceType))
            {
                ModelState.AddModelError(
                    nameof(model.DeviceType),
                    "Please select a valid device type.");
            }

            if (!AllowedStatuses.Contains(model.OperationalStatus))
            {
                ModelState.AddModelError(
                    nameof(model.OperationalStatus),
                    "Please select a valid operational status.");
            }

            bool farmExists = await AccessibleFarms()
                .AnyAsync(f => f.Id == model.FarmId);

            if (!farmExists)
            {
                ModelState.AddModelError(
                    nameof(model.FarmId),
                    "Please select a farm you are allowed to manage.");
            }
        }

        // GET: Devices/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await AccessibleDevices()
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DeviceDetailsViewModel
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    OperationalStatus = d.OperationalStatus,
                    FarmId = d.FarmId,
                    FarmName = d.Farm != null
                        ? d.Farm.FarmName
                        : "Not Assigned",
                    LastUpdated = d.LastUpdated
                })
                .FirstOrDefaultAsync();

            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await AccessibleDevices()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                return NotFound();
            }

            _context.Devices.Remove(device);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
