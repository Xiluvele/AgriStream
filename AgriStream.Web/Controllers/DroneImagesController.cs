
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AgriStream.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin,Farmer")]
    public class DroneImagesController : Controller
    {
        private readonly BlobContainerClient _container;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public DroneImagesController(
            IConfiguration config,
            ApplicationDbContext db,
            UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;

            var connection = config["AzureStorage:ConnectionString"];
            var container = config["AzureStorage:ContainerName"]
                ?? "drone-images";

            _container = new BlobContainerClient(
                connection, container);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _users.GetUserId(User);

            var farms = _db.Farms.AsNoTracking();

            if (!User.IsInRole("Admin"))
                farms = farms.Where(f => f.OwnerId == userId);

            ViewBag.Farms = await farms
                .OrderBy(f => f.FarmName)
                .Select(f => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.FarmName
                })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Upload(
            int farmId, IFormFile image)
        {
            var userId = _users.GetUserId(User);

            bool allowed = await _db.Farms.AnyAsync(f =>
                f.Id == farmId &&
                (User.IsInRole("Admin") || f.OwnerId == userId));

            if (!allowed)
                return Forbid();

            if (image == null || image.Length == 0 ||
                image.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Select an image under 5 MB.";
                return RedirectToAction(nameof(Index));
            }

            var allowedTypes = new Dictionary<string, string>
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp"
            };

            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (!allowedTypes.TryGetValue(extension, out var contentType) ||
                !string.Equals(image.ContentType, contentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only JPG, PNG and WebP images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            await _container.CreateIfNotExistsAsync(
                PublicAccessType.None);

            var blobName = $"farm-{farmId}/{Guid.NewGuid():N}{extension}";
            var blob = _container.GetBlobClient(blobName);

            await using var stream = image.OpenReadStream();

            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            });

            TempData["Success"] = "Image uploaded to Azure Blob Storage.";
            return RedirectToAction(nameof(Index));
        }
    }
}
