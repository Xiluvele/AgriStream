
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgriStream.Web.ViewModels.Devices
{
    public class DeviceFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Device name is required.")]
        [StringLength(150)]
        [Display(Name = "Device Name")]
        public string DeviceName { get; set; } = "";

        [Required(ErrorMessage = "Device type is required.")]
        [Display(Name = "Device Type")]
        public string DeviceType { get; set; } = "";

        [Required(ErrorMessage = "Select a status.")]
        [Display(Name = "Operational Status")]
        public string OperationalStatus { get; set; } = "Inactive";

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a farm.")]
        [Display(Name = "Assigned Farm")]
        public int FarmId { get; set; }

        public List<SelectListItem> Farms { get; set; } = new();
    }

    public class DeviceListItemViewModel
    {
        public int Id { get; set; }

        public string DeviceName { get; set; } = "";

        public string DeviceType { get; set; } = "";

        public string OperationalStatus { get; set; } = "";

        public string FarmName { get; set; } = "";

        public DateTime LastUpdated { get; set; }
    }

    public class DeviceIndexViewModel
    {
        public List<DeviceListItemViewModel> Devices { get; set; } = new();

        public string? Search { get; set; }

        public string? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalDevices { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalDevices / PageSize);
    }

    public class DeviceDetailsViewModel : DeviceListItemViewModel
    {
        public int FarmId { get; set; }
    }
}
