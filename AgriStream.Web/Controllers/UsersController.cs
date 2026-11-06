
using AgriStream.Web.Data;
using AgriStream.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ApplicationUser = AgriStream.Web.Models.ApplicationUser;

namespace AgriStream.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private static readonly string[] ManagedRoles =
        {
            "Admin",
            "Agronomist",
            "Farmer"
        };

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        public async Task<IActionResult> Index(
            string? search,
            string? roleFilter,
            int page = 1)
        {
            const int pageSize = 10;

            page = Math.Max(page, 1);

            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(u =>
                    u.Email != null &&
                    u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                if (!ManagedRoles.Contains(roleFilter))
                {
                    roleFilter = null;
                }
                else
                {
                    var userIdsInRole = _context.UserRoles
                        .Join(
                            _context.Roles,
                            ur => ur.RoleId,
                            r => r.Id,
                            (ur, r) => new
                            {
                                ur.UserId,
                                RoleName = r.Name
                            })
                        .Where(x => x.RoleName == roleFilter)
                        .Select(x => x.UserId);

                    query = query.Where(u =>
                        userIdsInRole.Contains(u.Id));
                }
            }

            int totalUsers = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalUsers / (double)pageSize);

            if (totalPages > 0)
            {
                page = Math.Min(page, totalPages);
            }

            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.EmailConfirmed
                })
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var userRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles
                    on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new
                {
                    ur.UserId,
                    RoleName = r.Name
                }
            ).ToListAsync();

            var model = new UserManagementViewModel
            {
                Search = search,
                RoleFilter = roleFilter,
                Page = page,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                AvailableRoles = ManagedRoles.ToList(),

                Users = users.Select(u => new UserListItemViewModel
                {
                    Id = u.Id,
                    Email = u.Email ?? "No Email",
                    EmailConfirmed = u.EmailConfirmed,

                    Role = string.Join(", ",
                        userRoles
                            .Where(r => r.UserId == u.Id)
                            .Select(r => r.RoleName)
                            .Where(r => r != null))
                }).ToList()
            };

            return View(model);
        }

        // GET: Users/EditRole/id
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                SelectedRole = roles.FirstOrDefault() ?? "Farmer",
                AvailableRoles = ManagedRoles.ToList()
            };

            return View(model);
        }

        // POST: Users/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(
            EditUserRoleViewModel model)
        {
            model.AvailableRoles = ManagedRoles.ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!ManagedRoles.Contains(model.SelectedRole))
            {
                ModelState.AddModelError(
                    nameof(model.SelectedRole),
                    "Please select a valid role.");

                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(
                model.SelectedRole))
            {
                ModelState.AddModelError(
                    nameof(model.SelectedRole),
                    "The selected role does not exist.");

                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return NotFound();
            }

            model.Email = user.Email ?? "";

            var currentRoles = await _userManager.GetRolesAsync(user);

            bool currentlyAdmin = currentRoles.Contains("Admin");

            bool removingAdmin =
                currentlyAdmin && model.SelectedRole != "Admin";

            string? currentUserId = _userManager.GetUserId(User);

            if (removingAdmin && user.Id == currentUserId)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You cannot remove your own Admin role.");

                return View(model);
            }

            if (removingAdmin)
            {
                var adminUsers =
                    await _userManager.GetUsersInRoleAsync("Admin");

                if (adminUsers.Count <= 1)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The last administrator cannot be removed.");

                    return View(model);
                }
            }

            // Remove roles managed by this application.
            var rolesToRemove = currentRoles
                .Where(r => ManagedRoles.Contains(r) &&
                            r != model.SelectedRole)
                .ToList();

            if (rolesToRemove.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }

            if (!await _userManager.IsInRoleAsync(
                user,
                model.SelectedRole))
            {
                var addResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        model.SelectedRole);

                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }

            TempData["SuccessMessage"] =
                $"Role updated successfully for {user.Email}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
