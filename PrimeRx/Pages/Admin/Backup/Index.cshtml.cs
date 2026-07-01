using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Backup;

public class IndexModel(BackupService backupService) : PageModel
{
    public List<BackupFileInfo> Backups { get; set; } = [];
    public string? Message { get; set; }
    public bool IsError { get; set; }

    public void OnGet()
    {
        Backups = backupService.ListBackups();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var path = await backupService.CreateBackupAsync();
            Message = $"Backup created: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = $"Backup failed: {ex.Message}";
        }
        Backups = backupService.ListBackups();
        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        try
        {
            var bytes = await backupService.DownloadBackupAsync();
            var fileName = $"primerx_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            return new FileContentResult(bytes, "application/octet-stream") { FileDownloadName = fileName };
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = $"Download failed: {ex.Message}";
            Backups = backupService.ListBackups();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRestoreAsync(IFormFile? backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            IsError = true;
            Message = "Please select a valid backup file.";
            Backups = backupService.ListBackups();
            return Page();
        }

        var tempPath = Path.GetTempFileName() + ".db";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
                await backupFile.CopyToAsync(stream);

            await backupService.RestoreBackupAsync(tempPath);
            Message = "Database restored successfully. Please restart the application.";
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = $"Restore failed: {ex.Message}";
        }
        finally
        {
            if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
        }
        Backups = backupService.ListBackups();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreFromServerAsync(string filePath)
    {
        try
        {
            await backupService.RestoreBackupAsync(filePath);
            Message = "Database restored from server backup. Please restart the application.";
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = $"Restore failed: {ex.Message}";
        }
        Backups = backupService.ListBackups();
        return Page();
    }
}
