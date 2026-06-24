using System.IO.Compression;
using System.Diagnostics;

namespace PrimeRxUpdater;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("PrimeRx Updater Started");
            Console.WriteLine("=======================");

            if (args.Length == 0)
            {
                Console.WriteLine("Error: No arguments provided");
                Console.WriteLine("Usage: PrimeRxUpdater.exe <zipFilePath> <installPath>");
                return;
            }

            string zipFilePath = args[0];
            string installPath = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();

            Console.WriteLine($"Zip file: {zipFilePath}");
            Console.WriteLine($"Install path: {installPath}");

            if (!File.Exists(zipFilePath))
            {
                Console.WriteLine($"Error: Zip file not found: {zipFilePath}");
                return;
            }

            if (!Directory.Exists(installPath))
            {
                Console.WriteLine($"Error: Install path not found: {installPath}");
                return;
            }

            // Step 1: Backup database
            Console.WriteLine("\nStep 1: Backing up database...");
            bool backupSuccess = await BackupDatabaseAsync(installPath);
            
            if (!backupSuccess)
            {
                Console.WriteLine("Warning: Database backup failed, but continuing with update...");
            }
            else
            {
                Console.WriteLine("Database backup completed successfully");
            }

            // Step 2: Extract update files
            Console.WriteLine("\nStep 2: Extracting update files...");
            bool extractSuccess = await ExtractUpdateAsync(zipFilePath, installPath);
            
            if (!extractSuccess)
            {
                Console.WriteLine("Error: Failed to extract update files");
                return;
            }
            Console.WriteLine("Update files extracted successfully");

            // Step 3: Cleanup
            Console.WriteLine("\nStep 3: Cleaning up...");
            try
            {
                File.Delete(zipFilePath);
                Console.WriteLine("Zip file deleted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete zip file: {ex.Message}");
            }

            // Step 4: Restart PrimeRx
            Console.WriteLine("\nStep 4: Restarting PrimeRx...");
            await RestartPrimeRxAsync(installPath);

            Console.WriteLine("\nUpdate completed successfully!");
            Console.WriteLine("PrimeRx should be starting now...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFatal error during update: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    static async Task<bool> BackupDatabaseAsync(string installPath)
    {
        try
        {
            string dataPath = Path.Combine(installPath, "Data");
            string backupsPath = Path.Combine(installPath, "Backups");

            // Create backups directory if it doesn't exist
            if (!Directory.Exists(backupsPath))
            {
                Directory.CreateDirectory(backupsPath);
            }

            // Find the database file
            string dbPath = Path.Combine(dataPath, "primerx.db");
            
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"Database file not found at: {dbPath}");
                return false;
            }

            // Create backup with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupPath = Path.Combine(backupsPath, $"primerx_{timestamp}.db");

            File.Copy(dbPath, backupPath, true);
            Console.WriteLine($"Database backed up to: {backupPath}");

            // Keep only last 10 backups
            var backups = Directory.GetFiles(backupsPath, "primerx_*.db")
                .OrderByDescending(f => f)
                .Skip(10);

            foreach (var oldBackup in backups)
            {
                try
                {
                    File.Delete(oldBackup);
                    Console.WriteLine($"Deleted old backup: {oldBackup}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete old backup {oldBackup}: {ex.Message}");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error backing up database: {ex.Message}");
            return false;
        }
    }

    static async Task<bool> ExtractUpdateAsync(string zipFilePath, string installPath)
    {
        try
        {
            // Wait a moment to ensure PrimeRx has fully closed
            await Task.Delay(2000);

            // Extract the zip file
            ZipFile.ExtractToDirectory(zipFilePath, installPath, true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting update: {ex.Message}");
            return false;
        }
    }

    static async Task RestartPrimeRxAsync(string installPath)
    {
        try
        {
            string exePath = Path.Combine(installPath, "PrimeRx.exe");

            if (!File.Exists(exePath))
            {
                Console.WriteLine($"PrimeRx.exe not found at: {exePath}");
                return;
            }

            // Wait a moment before starting
            await Task.Delay(1000);

            // Start PrimeRx
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = installPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            Console.WriteLine($"PrimeRx started from: {exePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restarting PrimeRx: {ex.Message}");
        }
    }
}
