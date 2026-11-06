
using AgriStream.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Web.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext( DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Farm> Farms { get; set; }

        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating( ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Sensor telemetry is stored in Azure Table Storage,
            // not in the Azure SQL database.
            modelBuilder.Ignore<SensorReading>();
        }
    }
}
