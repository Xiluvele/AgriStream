
using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.ViewModels.Users
{
    public class UserManagementViewModel
    {
        public List<UserListItemViewModel> Users { get; set; } = new();

        public string? Search { get; set; }

        public string? RoleFilter { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalUsers { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalUsers / PageSize);

        public List<string> AvailableRoles { get; set; } = new();
    }

    public class UserListItemViewModel
    {
        public string Id { get; set; } = "";

        public string Email { get; set; } = "";

        public string Role { get; set; } = "";

        public bool EmailConfirmed { get; set; }
    }

    public class EditUserRoleViewModel
    {
        [Required]
        public string UserId { get; set; } = "";

        [Required]
        [Display(Name = "User Role")]
        public string SelectedRole { get; set; } = "";

        public string Email { get; set; } = "";

        public List<string> AvailableRoles { get; set; } = new();
    }
}
