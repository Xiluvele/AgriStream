
using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.ViewModels.Farms
{
    public class FarmFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Farm name is required.")]
        [StringLength(150)]
        [Display(Name = "Farm Name")]
        public string FarmName { get; set; } = "";

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200)]
        public string Location { get; set; } = "";
    }

    public class FarmListItemViewModel
    {
        public int Id { get; set; }

        public string FarmName { get; set; } = "";

        public string Location { get; set; } = "";

        public string OwnerEmail { get; set; } = "";

        public int DeviceCount { get; set; }
    }

    public class FarmIndexViewModel
    {
        public List<FarmListItemViewModel> Farms { get; set; } = new();

        public string? Search { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalFarms { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalFarms / PageSize);
    }

    public class FarmDetailsViewModel
    {
        public int Id { get; set; }

        public string FarmName { get; set; } = "";

        public string Location { get; set; } = "";

        public string OwnerEmail { get; set; } = "";

        public int DeviceCount { get; set; }
    }
}
