
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AgriStream.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = AgriStream.Web.Models.ApplicationUser;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

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

            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            var container = config["AzureStorage:ContainerName"]
                ?? "drone-images";

            _container = new BlobContainerClient(
                connection, container);
        }

        // INDEX - DISPLAY IMAGE GALLERY
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _users.GetUserId(User);

            var farmsQuery = _db.Farms.AsNoTracking();

            if (!User.IsInRole("Admin"))
            {
                farmsQuery = farmsQuery.Where(
                    f => f.OwnerId == userId);
            }

            var farms = await farmsQuery.ToListAsync();

            var images = new List<DroneImageItem>();

            if (await _container.ExistsAsync())
            {
                foreach (var farm in farms)
                {
                    var prefix = $"farm-{farm.Id}/";



                    await foreach (var item in _container.GetBlobsAsync(
                        traits: BlobTraits.None,
                        states: BlobStates.None,
                        prefix: prefix,
                        cancellationToken: HttpContext.RequestAborted))
                    {
                        var blob = _container.GetBlobClient(item.Name);

                        if (!blob.CanGenerateSasUri)
                            continue;

                        var imageUrl = blob.GenerateSasUri(
                            BlobSasPermissions.Read,
                            DateTimeOffset.UtcNow.AddMinutes(30));

                        images.Add(new DroneImageItem
                        {
                            FileName = Path.GetFileName(item.Name),
                            ImageUrl = imageUrl.ToString(),
                            FarmName = farm.FarmName,
                            UploadedAt = item.Properties.CreatedOn
                        });
                    }


                }
            }

            return View(images
                .OrderByDescending(x => x.UploadedAt)
                .ToList());
        }

        // CREATE - DISPLAY UPLOAD FORM
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _users.GetUserId(User);

            var farms = _db.Farms.AsNoTracking();

            if (!User.IsInRole("Admin"))
            {
                farms = farms.Where(f => f.OwnerId == userId);
            }

            ViewBag.Farms = await farms
                .OrderBy(f => f.FarmName)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.FarmName
                })
                .ToListAsync();

            return View();
        }

        // UPLOAD - SAVE TO AZURE BLOB STORAGE
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
                return RedirectToAction(nameof(Create));
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
                !string.Equals(
                    image.ContentType,
                    contentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only JPG, JPEG, PNG and WebP images are allowed.";

                return RedirectToAction(nameof(Create));
            }

            // Check actual file signature
            var signature = new byte[12];
            int bytesRead;

            await using (var checkStream = image.OpenReadStream())
            {
                bytesRead = await checkStream.ReadAsync(
                    signature.AsMemory());
            }

            if (!IsValidImageSignature(
                signature.AsSpan(0, bytesRead), extension))
            {
                TempData["Error"] =
                    "The uploaded file is not a valid image.";

                return RedirectToAction(nameof(Create));
            }

            await _container.CreateIfNotExistsAsync(
                PublicAccessType.None);

            var blobName =
                $"farm-{farmId}/{Guid.NewGuid():N}{extension}";

            var blob = _container.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await using (var stream = image.OpenReadStream())
            {
                await blob.UploadAsync(stream, options);
            }

            TempData["Success"] =
                "Image uploaded successfully to Azure Blob Storage.";

            return RedirectToAction(nameof(Index));
        }

        // VALIDATE IMAGE SIGNATURE
        private static bool IsValidImageSignature(
            ReadOnlySpan<byte> data,
            string extension)
        {
            if (extension == ".jpg" || extension == ".jpeg")
            {
                return data.Length >= 3 &&
                       data[0] == 0xFF &&
                       data[1] == 0xD8 &&
                       data[2] == 0xFF;
            }

            if (extension == ".png")
            {
                byte[] pngSignature =
                {
                    0x89, 0x50, 0x4E, 0x47,
                    0x0D, 0x0A, 0x1A, 0x0A
                };

                return data.StartsWith(pngSignature);
            }

            if (extension == ".webp")
            {
                return data.Length >= 12 &&
                       data[0] == 'R' &&
                       data[1] == 'I' &&
                       data[2] == 'F' &&
                       data[3] == 'F' &&
                       data[8] == 'W' &&
                       data[9] == 'E' &&
                       data[10] == 'B' &&
                       data[11] == 'P';
            }

            return false;
        }
    }

    public class DroneImageItem
    {
        public string FileName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string FarmName { get; set; } = "";
        public DateTimeOffset? UploadedAt { get; set; }
    }
}
