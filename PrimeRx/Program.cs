using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Data.Seeder;
using PrimeRx.Helpers;
using PrimeRx.Middleware;
using PrimeRx.Models;
using PrimeRx.Services;
using Serilog;
using Serilog.Events;

var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(logsDir, "primerx-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var sqliteConnection = DatabasePath.ResolveSqliteConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Environment.ContentRootPath);

// Persist Data Protection keys to a stable, publish-independent location so
// login cookies, antiforgery tokens, and other protected data survive
// republishing and work identically under `dotnet run` and the published exe.
// Using ContentRootPath was broken because `dotnet run` and the publish folder
// have different ContentRootPaths, creating two separate key-rings — the
// published exe could never decrypt tokens generated during `dotnet run` (and
// vice-versa), silently breaking every POST form.
var dataProtectionDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PrimeRx", "DataProtection", "keys");
Directory.CreateDirectory(dataProtectionDir);
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDir))
    .SetApplicationName("PrimeRx");

if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
{
    dataProtectionBuilder.ProtectKeysWithDpapi();
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnection)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
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
    options.Conventions.AuthorizeFolder("/Purchase", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Billing", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Dashboard", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Inventory", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Due", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Reports", "StaffAccess");
    options.Conventions.AuthorizePage("/Reports/AuditFinancial", "AdminOnly");
    options.Conventions.AuthorizePage("/Reports/VendorReport", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Expenses", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Notifications", "StaffAccess");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Setup/Index");
    options.Conventions.AllowAnonymousToPage("/Index");
});

builder.Services.AddTransient<IEmailSender<IdentityUser>, EmailSender>();
builder.Services.AddSingleton<OtpStore>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<DueService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<PurchaseReturnService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<PayableService>();
builder.Services.AddScoped<AgingDueService>();
builder.Services.AddScoped<AuditFinancialService>();
builder.Services.AddSingleton<PdfGenerator>();
builder.Services.AddScoped<MedicineMasterService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UpdateService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await RoleSeeder.SeedAsync(services.GetRequiredService<RoleManager<IdentityRole>>());

    var masterService = services.GetRequiredService<MedicineMasterService>();
    var nepalDbPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "templates", "Nepal_5K_Medicine_Database.xlsx");
    await masterService.SeedFromNepalDatabaseAsync(nepalDbPath);
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
        
        if (launchUrl.Contains("0.0.0.0"))
        {
            launchUrl = launchUrl.Replace("0.0.0.0", "localhost");
        }

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

// Check for updates in background
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var updateService = scope.ServiceProvider.GetRequiredService<UpdateService>();
        
        // Wait a bit for the app to fully start
        await Task.Delay(5000);
        
        var updateInfo = await updateService.CheckForUpdatesAsync();
        
        if (updateInfo.UpdateAvailable)
        {
            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine($"UPDATE AVAILABLE");
            Console.WriteLine($"{'='*60}");
            Console.WriteLine($"Current Version: {updateInfo.CurrentVersion}");
            Console.WriteLine($"Latest Version:  {updateInfo.LatestVersion}");
            Console.WriteLine($"Download Size:   {updateInfo.DownloadSize / 1024 / 1024:F2} MB");
            Console.WriteLine($"\nRelease Notes:");
            Console.WriteLine(updateInfo.ReleaseNotes ?? "No release notes available.");
            Console.WriteLine($"\nTo update, visit: {updateInfo.ReleaseUrl}");
            Console.WriteLine($"{'='*60}\n");
        }
    }
    catch (Exception ex)
    {
        // Update checks are best-effort and expected to fail when no releases
        // repo/network is configured (e.g. local dev). Log at debug level only.
        Log.Debug(ex, "Update check skipped (non-fatal)");
    }
});

app.Run();
