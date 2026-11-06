
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE CONNECTION - AZURE SQL
// ==========================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(  "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>  options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// ==========================================
// 2. IDENTITY AND ROLE MANAGEMENT
// ==========================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// ==========================================
// 3. MVC AND RAZOR PAGES
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// ==========================================
// 4. BUILD APPLICATION
// ==========================================

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedRolesAsync(
        scope.ServiceProvider);
}



// ==========================================
// 5. ERROR HANDLING
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// ==========================================
// 6. MIDDLEWARE
// ==========================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();


// ==========================================
// 7. MVC ROUTING
// ==========================================

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}")
//    .WithStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


// ==========================================
// 8. IDENTITY RAZOR PAGES
// ==========================================

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
