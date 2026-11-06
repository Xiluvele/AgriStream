
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using AgriStream.Web.ViewModels.Farms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer")]
    public class FarmsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FarmsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Restrict non-admin users to their own farms.
        private IQueryable<Farm> AccessibleFarms()
        {
            var query = _context.Farms.AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                string? userId = _userManager.GetUserId(User);

                query = query.Where(f => f.OwnerId == userId);
            }

            return query;
        }

        // GET: Farms
        public async Task<IActionResult> Index(
            string? search,
            int page = 1)
        {
            const int pageSize = 10;

            page = Math.Max(page, 1);

            var query = AccessibleFarms().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(f =>
                    f.FarmName.Contains(search) ||
                    f.Location.Contains(search));
            }

            int totalFarms = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalFarms / (double)pageSize);

            if (totalPages > 0)
            {
                page = Math.Min(page, totalPages);
            }

            var farms = await query
                .OrderBy(f => f.FarmName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FarmListItemViewModel
                {
                    Id = f.Id,
                    FarmName = f.FarmName,
                    Location = f.Location,
                    OwnerEmail = f.Owner != null
                        ? f.Owner.Email ?? "Not Available"
                        : "Not Assigned",
                    DeviceCount = _context.Devices.Count(d =>
                        d.FarmId == f.Id)
                })
                .ToListAsync();

            var model = new FarmIndexViewModel
            {
                Farms = farms,
                Search = search,
                Page = page,
                PageSize = pageSize,
                TotalFarms = totalFarms
            };

            return View(model);
        }

        // GET: Farms/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var farm = await AccessibleFarms()
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new FarmDetailsViewModel
                {
                    Id = f.Id,
                    FarmName = f.FarmName,
                    Location = f.Location,
                    OwnerEmail = f.Owner != null
                        ? f.Owner.Email ?? "Not Available"
                        : "Not Assigned",
                    DeviceCount = _context.Devices.Count(d =>
                        d.FarmId == f.Id)
                })
                .FirstOrDefaultAsync();

            if (farm == null)
            {
                return NotFound();
            }

            return View(farm);
        }

        // GET: Farms/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new FarmFormViewModel());
        }

        // POST: Farms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            FarmFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var farm = new Farm
            {
                FarmName = model.FarmName.Trim(),
                Location = model.Location.Trim(),
                OwnerId = userId
            };

            _context.Farms.Add(farm);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Farm registered successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Farms/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var farm = await AccessibleFarms()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (farm == null)
            {
                return NotFound();
            }

            return View(new FarmFormViewModel
            {
                Id = farm.Id,
                FarmName = farm.FarmName,
                Location = farm.Location
            });
        }

        // POST: Farms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            FarmFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var farm = await AccessibleFarms()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (farm == null)
            {
                return NotFound();
            }

            farm.FarmName = model.FarmName.Trim();
            farm.Location = model.Location.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Farm updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Farms/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var farm = await AccessibleFarms()
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new FarmDetailsViewModel
                {
                    Id = f.Id,
                    FarmName = f.FarmName,
                    Location = f.Location,
                    OwnerEmail = f.Owner != null
                        ? f.Owner.Email ?? "Not Available"
                        : "Not Assigned",
                    DeviceCount = _context.Devices.Count(d =>
                        d.FarmId == f.Id)
                })
                .FirstOrDefaultAsync();

            if (farm == null)
            {
                return NotFound();
            }

            return View(farm);
        }

        // POST: Farms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var farm = await AccessibleFarms()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (farm == null)
            {
                return NotFound();
            }

            bool hasDevices = await _context.Devices
                .AnyAsync(d => d.FarmId == id);

            if (hasDevices)
            {
                TempData["ErrorMessage"] =
                    "This farm cannot be deleted because it has " +
                    "registered devices. Remove or reassign the devices first.";

                return RedirectToAction(nameof(Index));
            }

            _context.Farms.Remove(farm);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Farm deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
