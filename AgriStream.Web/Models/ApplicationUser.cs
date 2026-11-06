using Microsoft.AspNetCore.Identity;

namespace AgriStream.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    }
}
