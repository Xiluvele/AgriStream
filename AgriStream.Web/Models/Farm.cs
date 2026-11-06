using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.Models
{
    public class Farm
    {
        public int Id { get; set; }

        [Required]
        public string FarmName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string? OwnerId { get; set; }

        public ApplicationUser? Owner { get; set; }
    }
}
