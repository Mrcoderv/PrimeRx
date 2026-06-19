using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Data.Seeder;
using PrimeRx.Helpers;
using PrimeRx.Middleware;
using PrimeRx.Models;
using PrimeRx.Services;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && !builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5000");
}

var sqliteConnection = DatabasePath.ResolveSqliteConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Environment.ContentRootPath);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnection));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffAccess", policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Staff));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(AppRoles.Admin));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Billing", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Dashboard", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Inventory", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Due", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Reports", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Setup/Index");
    options.Conventions.AllowAnonymousToPage("/Index");
});

builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<DueService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddSingleton<PdfGenerator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await RoleSeeder.SeedAsync(services.GetRequiredService<RoleManager<IdentityRole>>());
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
}

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? string.Empty;
if (urls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<SetupMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

if (!app.Environment.IsDevelopment()
    && !string.Equals(Environment.GetEnvironmentVariable("PRIMERX_NO_BROWSER"), "1", StringComparison.OrdinalIgnoreCase))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var launchUrl = app.Urls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? "http://localhost:5000";
        try
        {
            Process.Start(new ProcessStartInfo(launchUrl) { UseShellExecute = true });
        }
        catch
        {
            // Browser launch is best-effort; app still runs if it fails.
        }
    });
}

app.Run();
