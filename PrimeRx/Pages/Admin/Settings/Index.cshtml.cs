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

public class IndexModel(ApplicationDbContext context, UpdateService updateService) : PageModel
{
    [BindProperty]
    public CompanyProfile Settings { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; } = "company";

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
        }

        await context.SaveChangesAsync();
        return RedirectToPage(new { message = "Settings saved successfully.", tab = Tab ?? "company" });
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
