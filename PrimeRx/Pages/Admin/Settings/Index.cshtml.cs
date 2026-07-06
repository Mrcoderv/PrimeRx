using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Settings;

public class IndexModel(ApplicationDbContext context, UpdateService updateService, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public CompanyProfile Settings { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; } = "company";

    [BindProperty]
    public IFormFile? LogoUpload { get; set; }

    [BindProperty]
    public bool RemoveLogo { get; set; }

    public string? Message { get; set; }
    
    public UpdateService.UpdateInfo? UpdateInfo { get; set; }

    public async Task OnGetAsync(string? message)
    {
        Message = message;
        Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
        
        // Check for updates when on the updates tab
        if (Tab == "updates")
        {
            UpdateInfo = await updateService.CheckForUpdatesAsync();
        }
    }

    private async Task<string?> HandleLogoUploadAsync()
    {
        if (RemoveLogo)
            return null;

        if (LogoUpload == null || LogoUpload.Length == 0)
            return null;

        var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowed.Contains(LogoUpload.ContentType))
        {
            Message = "Invalid file type. Only PNG, JPG, and WebP are allowed.";
            return null;
        }

        if (LogoUpload.Length > 2 * 1024 * 1024)
        {
            Message = "File size exceeds 2 MB limit.";
            return null;
        }

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "logos");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(LogoUpload.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var fileName = $"company-logo{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await LogoUpload.CopyToAsync(stream);

        return $"/uploads/logos/{fileName}";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Tab = "company";
            return Page();
        }

        var existing = await context.CompanyProfiles.FirstOrDefaultAsync();
        if (existing is null)
        {
            Settings.LogoPath = await HandleLogoUploadAsync() ?? Settings.LogoPath;
            context.CompanyProfiles.Add(Settings);
        }
        else
        {
            existing.Name = Settings.Name;
            existing.Address = Settings.Address;
            existing.Phone = Settings.Phone;
            existing.PAN = Settings.PAN;
            existing.GSTIN = Settings.GSTIN;
            existing.TaxRate = Settings.TaxRate;
            existing.TaxLabel = Settings.TaxLabel;
            existing.TaxInclusive = Settings.TaxInclusive;
            existing.BillTitle = Settings.BillTitle;
            existing.BillFooterText = Settings.BillFooterText;
            existing.BillPrimaryColor = Settings.BillPrimaryColor;
            existing.ShowPanOnBill = Settings.ShowPanOnBill;
            existing.ShowGstinOnBill = Settings.ShowGstinOnBill;
            existing.DefaultDiscountMarginPercent = Settings.DefaultDiscountMarginPercent;

            var newLogoPath = await HandleLogoUploadAsync();
            if (RemoveLogo || newLogoPath != null)
            {
                if (!string.IsNullOrEmpty(existing.LogoPath))
                {
                    var oldPath = Path.Combine(env.WebRootPath, existing.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
                existing.LogoPath = newLogoPath;
            }
        }

        await context.SaveChangesAsync();
        return RedirectToPage(new { message = Message ?? "Settings saved successfully.", tab = Tab ?? "company" });
    }

    public async Task<IActionResult> OnPostCheckUpdatesAsync()
    {
        Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
        UpdateInfo = await updateService.CheckForUpdatesAsync();
        Tab = "updates";
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var updateInfo = await updateService.CheckForUpdatesAsync();
        
        if (!updateInfo.UpdateAvailable || string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            Message = "No update available or download URL not found.";
            Tab = "updates";
            Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
            UpdateInfo = updateInfo;
            return Page();
        }

        // Download the update
        string tempPath = Path.Combine(Path.GetTempPath(), "PrimeRx-update.zip");
        var downloadedPath = await updateService.DownloadUpdateAsync(updateInfo.DownloadUrl, tempPath);
        
        if (string.IsNullOrEmpty(downloadedPath) || !System.IO.File.Exists(downloadedPath))
        {
            Message = "Failed to download update.";
            Tab = "updates";
            Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
            UpdateInfo = updateInfo;
            return Page();
        }

        // Path to the updater executable
        string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrimeRxUpdater.exe");
        
        if (!System.IO.File.Exists(updaterPath))
        {
            Message = "Updater executable not found. Please ensure PrimeRxUpdater.exe is in the application directory.";
            Tab = "updates";
            Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
            UpdateInfo = updateInfo;
            return Page();
        }

        // Start the updater process
        var installPath = AppDomain.CurrentDomain.BaseDirectory;
        var startInfo = new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = $"\"{downloadedPath}\" \"{installPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        try
        {
            Process.Start(startInfo);
            
            // Shutdown the current application
            Environment.Exit(0);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            Message = $"Failed to start updater: {ex.Message}";
            Tab = "updates";
            Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
            UpdateInfo = updateInfo;
            return Page();
        }
    }
}
