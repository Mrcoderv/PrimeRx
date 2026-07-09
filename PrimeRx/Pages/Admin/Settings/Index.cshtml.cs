using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Settings;

public class IndexModel(
    ApplicationDbContext context,
    UpdateService updateService,
    IWebHostEnvironment env,
    ILogger<IndexModel> logger) : PageModel
{
    // ── Page state ────────────────────────────────────────────────────────────

    [BindProperty]
    public CompanyProfile Settings { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; } = "company";

    [BindProperty]
    public IFormFile? LogoUpload { get; set; }

    [BindProperty]
    public bool RemoveLogo { get; set; }

    public string?                  Message    { get; set; }
    public UpdateService.UpdateInfo? UpdateInfo { get; set; }

    // ── GET ───────────────────────────────────────────────────────────────────

    public async Task OnGetAsync(string? message)
    {
        Message  = message;
        Settings = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();

        if (Tab == "updates")
            UpdateInfo = await updateService.CheckForUpdatesAsync();
    }

    // ── POST: company settings ────────────────────────────────────────────────

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
            existing.Name                       = Settings.Name;
            existing.Address                    = Settings.Address;
            existing.Phone                      = Settings.Phone;
            existing.PAN                        = Settings.PAN;
            existing.GSTIN                      = Settings.GSTIN;
            existing.TaxRate                    = Settings.TaxRate;
            existing.TaxLabel                   = Settings.TaxLabel;
            existing.TaxInclusive               = Settings.TaxInclusive;
            existing.BillTitle                  = Settings.BillTitle;
            existing.BillFooterText             = Settings.BillFooterText;
            existing.BillPrimaryColor           = Settings.BillPrimaryColor;
            existing.ShowPanOnBill              = Settings.ShowPanOnBill;
            existing.ShowGstinOnBill            = Settings.ShowGstinOnBill;
            existing.DefaultDiscountMarginPercent = Settings.DefaultDiscountMarginPercent;

            var newLogoPath = await HandleLogoUploadAsync();
            if (RemoveLogo || newLogoPath != null)
            {
                if (!string.IsNullOrEmpty(existing.LogoPath))
                {
                    var oldPath = Path.Combine(env.WebRootPath, existing.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                existing.LogoPath = newLogoPath;
            }
        }

        await context.SaveChangesAsync();
        return RedirectToPage(new { message = Message ?? "Settings saved successfully.", tab = Tab ?? "company" });
    }

    // ── POST: check for updates (manual refresh) ──────────────────────────────

    public async Task<IActionResult> OnPostCheckUpdatesAsync()
    {
        Settings   = await context.CompanyProfiles.SingleOrDefaultAsync() ?? new CompanyProfile();
        UpdateInfo = await updateService.CheckForUpdatesAsync();
        Tab        = "updates";
        return Page();
    }

    // ── POST: perform update ──────────────────────────────────────────────────

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        // Re-fetch update info so we always have the freshest metadata
        var updateInfo = await updateService.CheckForUpdatesAsync();

        if (!updateInfo.UpdateAvailable || string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            return UpdateError("No update is currently available.", updateInfo);
        }

        // ── 1. Locate the updater executable ─────────────────────────────────
        var updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrimeRxUpdater.exe");
        if (!System.IO.File.Exists(updaterPath))
        {
            return UpdateError(
                "PrimeRxUpdater.exe was not found in the application folder. " +
                "Please reinstall PrimeRx or download the update manually.",
                updateInfo);
        }

        // ── 2. Download the update zip ────────────────────────────────────────
        // Place the zip in a temp folder on the same drive as the install so the
        // updater can do an atomic rename without copying across volumes.
        var installBase = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parentDir  = Path.GetDirectoryName(installBase) ?? Path.GetTempPath();
        var zipPath    = Path.Combine(parentDir, $"PrimeRx-update-v{updateInfo.LatestVersion}.zip");

        logger.LogInformation("Downloading update v{Version} from {Url}", updateInfo.LatestVersion, updateInfo.DownloadUrl);

        UpdateService.UpdatePackage package;
        try
        {
            package = await updateService.PrepareUpdateAsync(updateInfo, zipPath);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("checksum", StringComparison.OrdinalIgnoreCase))
        {
            // Checksum mismatch — the zip has already been deleted by PrepareUpdateAsync
            logger.LogError(ex, "Checksum verification failed for update v{Version}", updateInfo.LatestVersion);
            return UpdateError(
                "The downloaded update file failed its integrity check (SHA-256 mismatch). " +
                "This could mean the file was corrupted during download. Please try again. " +
                "If the problem persists, contact Prime LogicTech support.",
                updateInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download update v{Version}", updateInfo.LatestVersion);
            TryDeleteFile(zipPath);
            return UpdateError($"Download failed: {ex.Message}", updateInfo);
        }

        logger.LogInformation(
            "Update package ready. ChecksumVerified={Verified}, Path={Path}",
            package.ChecksumVerified, package.ZipPath);

        // ── 3. Build updater arguments ────────────────────────────────────────
        var currentPid  = Environment.ProcessId;
        var argBuilder  = new System.Text.StringBuilder();
        argBuilder.Append($"\"{package.ZipPath}\" \"{installBase}\"");
        argBuilder.Append($" --version \"{updateInfo.LatestVersion}\"");
        argBuilder.Append($" --pid {currentPid}");

        if (package.ChecksumVerified && !string.IsNullOrWhiteSpace(package.VerifiedChecksum))
        {
            // The updater will re-verify — belt-and-suspenders
            argBuilder.Append($" --sha256 \"{package.VerifiedChecksum}\"");
        }

        // ── 4. Launch the updater and exit ────────────────────────────────────
        var psi = new ProcessStartInfo
        {
            FileName       = updaterPath,
            Arguments      = argBuilder.ToString(),
            UseShellExecute    = true,   // creates a visible console window staff can read
            CreateNoWindow     = false,
            WorkingDirectory   = installBase,
        };

        logger.LogInformation("Launching PrimeRxUpdater: {Args}", argBuilder);

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to launch PrimeRxUpdater");
            TryDeleteFile(zipPath);
            return UpdateError($"Could not start the updater process: {ex.Message}", updateInfo);
        }

        // Give the updater a moment to start before we exit
        await Task.Delay(500);

        // Shut down this instance — the updater will wait for us to exit
        logger.LogInformation("PrimeRx shutting down for update to v{Version}", updateInfo.LatestVersion);
        Environment.Exit(0);

        // Unreachable — satisfies the compiler
        return new EmptyResult();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IActionResult UpdateError(string message, UpdateService.UpdateInfo? info)
    {
        Message    = message;
        Tab        = "updates";
        UpdateInfo = info;
        Settings   = context.CompanyProfiles.FirstOrDefault() ?? new CompanyProfile();
        return Page();
    }

    private async Task<string?> HandleLogoUploadAsync()
    {
        if (RemoveLogo) return null;
        if (LogoUpload == null || LogoUpload.Length == 0) return null;

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

        var ext      = Path.GetExtension(LogoUpload.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var fileName = $"company-logo{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await LogoUpload.CopyToAsync(stream);

        return $"/uploads/logos/{fileName}";
    }

    private static void TryDeleteFile(string path)
    {
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch { /* best effort */ }
    }
}
