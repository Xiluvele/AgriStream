using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.Models
{
    public class Device
    {
        public int Id { get; set; }

        [Required]
        public string DeviceName { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string OperationalStatus { get; set; } = "Inactive";

        public DateTime LastUpdated { get; set; }
            = DateTime.UtcNow;

        public int FarmId { get; set; }

        public Farm? Farm { get; set; }
    }
}
