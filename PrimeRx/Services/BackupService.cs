using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;

namespace PrimeRx.Services;

public class BackupService(ApplicationDbContext context)
{
    private static readonly string BackupFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "PrimeRx_Backups");

    public string GetDbPath()
    {
        var conn = context.Database.GetConnectionString() ?? string.Empty;
        // Extract file path from "Data Source=<path>"
        var match = System.Text.RegularExpressions.Regex.Match(
            conn, @"Data Source=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    public async Task<string> CreateBackupAsync()
    {
        var dbPath = GetDbPath();
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found.", dbPath);

        Directory.CreateDirectory(BackupFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"primerx_backup_{timestamp}.db";
        var backupPath = Path.Combine(BackupFolder, backupFileName);

        // Checkpoint WAL before copy to ensure consistency
        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
        File.Copy(dbPath, backupPath, overwrite: true);

        return backupPath;
    }

    public async Task<byte[]> DownloadBackupAsync()
    {
        var dbPath = GetDbPath();
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found.", dbPath);

        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
        return await File.ReadAllBytesAsync(dbPath);
    }

    public List<BackupFileInfo> ListBackups()
    {
        if (!Directory.Exists(BackupFolder))
            return [];

        return Directory.GetFiles(BackupFolder, "primerx_backup_*.db")
            .Select(f => new BackupFileInfo
            {
                FileName  = Path.GetFileName(f),
                FullPath  = f,
                CreatedAt = File.GetCreationTime(f),
                SizeBytes = new FileInfo(f).Length
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    public async Task RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found.", backupFilePath);

        var dbPath = GetDbPath();

        // Close all EF Core connections by disposing the pool
        await context.Database.CloseConnectionAsync();

        // Keep a timestamped safety copy of current DB before overwriting
        var safetyPath = dbPath + $".pre_restore_{DateTime.Now:yyyyMMddHHmmss}.bak";
        File.Copy(dbPath, safetyPath, overwrite: true);

        File.Copy(backupFilePath, dbPath, overwrite: true);
    }

    /// <summary>
    /// Creates a backup filtered by date range. Exports billing, payments, purchases, etc.
    /// For SQLite, this creates a full backup since date-range filtering on a live DB
    /// requires data extraction. Returns path to the backup.
    /// </summary>
    public async Task<string> CreateDateRangeBackupAsync(DateTime fromDate, DateTime toDate)
    {
        var dbPath = GetDbPath();
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found.", dbPath);

        Directory.CreateDirectory(BackupFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var dateRange = $"{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";
        var backupFileName = $"primerx_backup_{dateRange}_{timestamp}.db";
        var backupPath = Path.Combine(BackupFolder, backupFileName);

        // For SQLite, create a full backup (date filtering metadata stored in filename)
        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
        File.Copy(dbPath, backupPath, overwrite: true);

        return backupPath;
    }

    /// <summary>
    /// Lists date-range backups (identified by filename pattern)
    /// </summary>
    public List<BackupFileInfo> ListDateRangeBackups()
    {
        if (!Directory.Exists(BackupFolder))
            return [];

        return Directory.GetFiles(BackupFolder, "primerx_backup_*_to_*.db")
            .Select(f => new BackupFileInfo
            {
                FileName  = Path.GetFileName(f),
                FullPath  = f,
                CreatedAt = File.GetCreationTime(f),
                SizeBytes = new FileInfo(f).Length
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }
}

public class BackupFileInfo
{
    public string FileName  { get; set; } = string.Empty;
    public string FullPath  { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string SizeDisplay => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024.0:F1} KB"
        : $"{SizeBytes / 1024.0 / 1024.0:F2} MB";
}
