
namespace AgriStream.Web.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalFarms { get; set; }

        public int TotalDevices { get; set; }

        public int ActiveDevices { get; set; }

        public int TotalUsers { get; set; }

        public string CurrentUserName { get; set; } = "";

        public string CurrentRole { get; set; } = "";

        public List<DashboardDeviceViewModel> RecentDevices { get; set; }= new();
        public List<IrrigationActivityViewModel> IrrigationActivities { get; set; } = new();
    }

    public class DashboardDeviceViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string DeviceType { get; set; } = "";

        public string FarmName { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime LastUpdated { get; set; }
    }

    public class IrrigationActivityViewModel
    {
        public int DeviceId { get; set; }

        public string Command { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTimeOffset RequestedAt { get; set; }
    }

}
