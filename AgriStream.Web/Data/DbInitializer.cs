
using Microsoft.AspNetCore.Identity;

namespace AgriStream.Web.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider .GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles =
            {
                "Admin",
                "Agronomist",
                "Farmer"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result =await roleManager.CreateAsync( new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new Exception(  $"Failed to create role: {role}");
                    }
                }
            }
        }
    }
}
