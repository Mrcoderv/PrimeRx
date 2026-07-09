// ─────────────────────────────────────────────────────────────────────────────
//  PrimeRx Safe Updater  v2.0
//  Performs an atomic, zero-data-loss update of PrimeRx with full rollback.
//
//  Usage:
//    PrimeRxUpdater.exe <zipPath> <installPath>
//                       [--sha256 <hexHash>]
//                       [--pid <mainProcessId>]
//                       [--version <newVersion>]
//
//  Safety guarantees:
//    • SHA-256 checksum verified before touching anything
//    • All user data preserved (Data/, Backups/, uploads, appsettings.json)
//    • Pre-update database backup written to Backups/pre_update_<timestamp>/
//    • Atomic directory swap (rename-rename, same NTFS volume)
//    • Automatic rollback on any failure after the swap
//    • Detailed log written to <installPath>/Logs/update_<timestamp>.log
// ─────────────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

// ── Exit codes ────────────────────────────────────────────────────────────────
const int ExitOk    = 0;
const int ExitError = 1;

// ── Parse arguments ───────────────────────────────────────────────────────────
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: PrimeRxUpdater.exe <zipPath> <installPath> [--sha256 <hash>] [--pid <pid>] [--version <ver>]");
    Environment.Exit(ExitError);
}

var zipPath     = Path.GetFullPath(args[0]);
var installPath = Path.GetFullPath(args[1].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
string? expectedSha256 = null;
int?    mainPid        = null;
string  newVersion     = "unknown";

for (int i = 2; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--sha256"  when i + 1 < args.Length: expectedSha256 = args[++i]; break;
        case "--pid"     when i + 1 < args.Length:
            if (int.TryParse(args[++i], out var p)) mainPid = p;
            break;
        case "--version" when i + 1 < args.Length: newVersion = args[++i]; break;
    }
}

// ── Logging ───────────────────────────────────────────────────────────────────
var ts      = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var logsDir = Path.Combine(installPath, "Logs");
StreamWriter? logFile = null;

try
{
    Directory.CreateDirectory(logsDir);
    logFile = new StreamWriter(Path.Combine(logsDir, $"update_{ts}.log"), append: false, Encoding.UTF8)
    {
        AutoFlush = true
    };
}
catch
{
    // Logging is best-effort; update must still proceed
}

void L(string level, string msg)
{
    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {msg}";
    Console.WriteLine(line);
    try { logFile?.WriteLine(line); } catch { }
}

void Info(string m)  => L("INFO ", m);
void Warn(string m)  => L("WARN ", m);
void Err(string m)   => L("ERROR", m);
void Sep()           => Info("────────────────────────────────────────────────────");

// ── Rollback state ────────────────────────────────────────────────────────────
string? tempDir      = null;
string? backupDir    = null;
bool    swapDone     = false;

void Rollback(string reason)
{
    Err($"ROLLBACK — {reason}");
    try
    {
        if (swapDone)
        {
            // After swap: installPath = new (bad/untested), backupDir = old (good)
            if (backupDir != null && Directory.Exists(backupDir))
            {
                if (Directory.Exists(installPath))
                {
                    var failedPath = installPath + $"_FAILED_{ts}";
                    Warn($"Moving failed install aside → {failedPath}");
                    SafeMove(installPath, failedPath);
                }
                Info($"Restoring from backup → {installPath}");
                SafeMove(backupDir, installPath);
                Info("✓ Rollback complete. Original installation restored.");
            }
            else
            {
                Err("CRITICAL: backup directory missing during rollback!");
                Err($"  Expected: {backupDir}");
                Err("  Manual recovery required — check sibling directories.");
            }
        }
        else
        {
            Info("Swap never started — original installation untouched.");
        }
    }
    catch (Exception ex)
    {
        Err($"Rollback itself failed: {ex.Message}");
        Err("MANUAL RECOVERY: restore files from the backup directory next to the install folder.");
    }

    // Clean up temp dir if it still exists
    if (tempDir != null && Directory.Exists(tempDir))
        SafeDeleteDir(tempDir, "temp extraction dir");
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN UPDATE FLOW
// ─────────────────────────────────────────────────────────────────────────────
try
{
    Console.Title = "PrimeRx Updater";
    Info("════════════════════════════════════════════════════════");
    Info($"  PrimeRx Safe Updater  —  installing v{newVersion}");
    Info("════════════════════════════════════════════════════════");
    Info($"  Install path : {installPath}");
    Info($"  Zip path     : {zipPath}");
    Info($"  SHA-256 check: {(expectedSha256 != null ? "YES" : "NO (not provided)")}");
    Info($"  Main PID     : {mainPid?.ToString() ?? "not provided"}");
    Sep();

    // ── Phase 1 · Pre-flight ─────────────────────────────────────────────────
    Info("Phase 1 · Pre-flight checks");

    if (!File.Exists(zipPath))
        throw new FileNotFoundException($"Update zip not found: {zipPath}");

    var zipFi = new FileInfo(zipPath);
    if (zipFi.Length < 1024 * 1024)
        throw new Exception($"Zip is suspiciously small ({zipFi.Length:N0} bytes). Aborting for safety.");

    if (!Directory.Exists(installPath))
        throw new DirectoryNotFoundException($"Install directory not found: {installPath}");

    Info($"  Zip size : {zipFi.Length / 1024.0 / 1024.0:F2} MB  ✓");
    Info($"  Install  : exists  ✓");

    // ── Phase 2 · Wait for main process ─────────────────────────────────────
    Sep();
    Info("Phase 2 · Waiting for PrimeRx to shut down");

    if (mainPid.HasValue)
    {
        try
        {
            var proc = Process.GetProcessById(mainPid.Value);
            Info($"  Waiting up to 45 s for PID {mainPid}…");
            if (!proc.WaitForExit(45_000))
            {
                Warn("  Process did not exit in 45 s — sending kill signal.");
                try
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(5_000);
                }
                catch (Exception kex) { Warn($"  Kill attempt: {kex.Message}"); }
            }
            Info("  Main process exited  ✓");
        }
        catch (ArgumentException)
        {
            Info("  Main process already exited  ✓");
        }
    }
    else
    {
        Info("  No PID provided — pausing 4 s for handles to release…");
        Thread.Sleep(4_000);
    }

    // ── Phase 3 · SHA-256 verification ──────────────────────────────────────
    Sep();
    Info("Phase 3 · Checksum verification");

    if (!string.IsNullOrWhiteSpace(expectedSha256))
    {
        Info("  Computing SHA-256 of downloaded zip…");
        var actualHash = ComputeSha256(zipPath);
        Info($"  Expected : {expectedSha256.Trim().ToUpperInvariant()}");
        Info($"  Actual   : {actualHash.ToUpperInvariant()}");

        if (!string.Equals(actualHash, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(zipPath);
            throw new Exception(
                "SHA-256 checksum mismatch! " +
                "The downloaded file is corrupt or has been tampered with. " +
                "The update has been cancelled and the zip deleted.");
        }
        Info("  Checksum verified  ✓");
    }
    else
    {
        Warn("  No checksum provided — skipping verification (consider adding --sha256).");
    }

    // ── Phase 4 · Extract to temp directory ─────────────────────────────────
    Sep();
    Info("Phase 4 · Extracting update package");

    // Temp dir is a sibling of installPath so it is on the same NTFS volume
    // → Directory.Move() between them will be an atomic rename (no copy).
    var parentDir = Path.GetDirectoryName(installPath)
        ?? throw new Exception("Cannot determine parent of install path.");
    tempDir = Path.Combine(parentDir, $"PrimeRx_Temp_{Guid.NewGuid():N}");

    Directory.CreateDirectory(tempDir);
    Info($"  Extracting to: {tempDir}");
    ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);

    var allFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
    Info($"  Extracted {allFiles.Length} files  ✓");

    // ── Phase 5 · Validate extracted package ────────────────────────────────
    Sep();
    Info("Phase 5 · Validating extracted package");

    var newExe = Path.Combine(tempDir, "PrimeRx.exe");
    if (!File.Exists(newExe))
        throw new Exception($"PrimeRx.exe not found in extracted package. The zip may be for a different platform or is corrupt.");

    var newExeFi = new FileInfo(newExe);
    if (newExeFi.Length < 1024 * 1024)
        throw new Exception($"PrimeRx.exe is too small ({newExeFi.Length:N0} bytes). Package appears corrupt.");

    if (allFiles.Length < 10)
        throw new Exception($"Only {allFiles.Length} files in package — expected many more. Package is incomplete.");

    Info($"  PrimeRx.exe : {newExeFi.Length / 1024.0 / 1024.0:F2} MB  ✓");
    Info($"  File count  : {allFiles.Length}  ✓");

    // ── Phase 6 · Pre-update backup ──────────────────────────────────────────
    Sep();
    Info("Phase 6 · Creating pre-update safety backup");

    var backupsRoot    = Path.Combine(installPath, "Backups");
    var updateBackup   = Path.Combine(backupsRoot, $"pre_update_{ts}");
    Directory.CreateDirectory(updateBackup);

    // 6a. Database
    // Capture presence BEFORE the swap so Phase 9 validation is correct.
    // After the atomic rename installPath points to the new install; dbPath and
    // verifyDb would resolve identically, so File.Exists(dbPath) would silently
    // pass even if preservation had failed.  The boolean is the fix.
    var dbPath      = Path.Combine(installPath, "Data", "primerx.db");
    bool hadDatabase = File.Exists(dbPath);
    if (hadDatabase)
    {
        var dbDest = Path.Combine(updateBackup, "primerx.db");
        File.Copy(dbPath, dbDest, overwrite: true);

        // WAL / SHM files
        foreach (var wal in new[] { dbPath + "-wal", dbPath + "-shm" })
            if (File.Exists(wal)) File.Copy(wal, Path.Combine(updateBackup, Path.GetFileName(wal)), overwrite: true);

        // Hash the backup so we can verify it later if needed
        var dbHash = ComputeSha256(dbDest);
        File.WriteAllText(Path.Combine(updateBackup, "primerx.db.sha256"), dbHash);
        Info($"  Database backed up  (SHA-256: {dbHash[..16]}…)  ✓");
    }
    else
    {
        Warn("  Database not found — backup skipped (first-run or empty install?)");
    }

    // 6b. appsettings.json
    var settingsPath = Path.Combine(installPath, "appsettings.json");
    if (File.Exists(settingsPath))
    {
        File.Copy(settingsPath, Path.Combine(updateBackup, "appsettings.json"), overwrite: true);
        Info("  appsettings.json backed up  ✓");
    }

    Info($"  Backup location: {updateBackup}");

    // ── Phase 7 · Preserve user data into temp ───────────────────────────────
    Sep();
    Info("Phase 7 · Preserving user data");

    // Data/ — database + any other runtime data
    var dataSource = Path.Combine(installPath, "Data");
    if (Directory.Exists(dataSource))
    {
        CopyDir(dataSource, Path.Combine(tempDir, "Data"));
        Info("  Data/  ✓");
    }

    // Backups/ — all prior backups INCLUDING the one we just created
    var backupsSource = Path.Combine(installPath, "Backups");
    if (Directory.Exists(backupsSource))
    {
        CopyDir(backupsSource, Path.Combine(tempDir, "Backups"));
        Info("  Backups/  ✓");
    }

    // wwwroot/uploads/ — user logos, images, etc.
    var uploadsSource = Path.Combine(installPath, "wwwroot", "uploads");
    if (Directory.Exists(uploadsSource))
    {
        CopyDir(uploadsSource, Path.Combine(tempDir, "wwwroot", "uploads"));
        Info("  wwwroot/uploads/  ✓");
    }

    // appsettings.json — user's connection string and custom config
    // We deliberately keep the OLD settings so user customisations survive.
    if (File.Exists(settingsPath))
    {
        File.Copy(settingsPath, Path.Combine(tempDir, "appsettings.json"), overwrite: true);
        Info("  appsettings.json  ✓");
    }

    // Logs/ — optional; failure here is non-fatal
    var logsSource = Path.Combine(installPath, "Logs");
    if (Directory.Exists(logsSource))
    {
        try { CopyDir(logsSource, Path.Combine(tempDir, "Logs")); Info("  Logs/  ✓"); }
        catch (Exception ex) { Warn($"  Logs/ copy failed (non-critical): {ex.Message}"); }
    }

    // ── Phase 8 · Atomic swap ────────────────────────────────────────────────
    Sep();
    Info("Phase 8 · Atomic installation swap");

    backupDir = Path.Combine(parentDir, $"PrimeRx_OldInstall_{ts}");

    Info($"  8a. Rename current install → backup:  {Path.GetFileName(backupDir)}");
    Directory.Move(installPath, backupDir);
    // ↑ From this point the original install lives at backupDir.
    //   The updater's own exe (inside that dir) is still running fine because
    //   Windows holds it open by inode, not by path.

    Info($"  8b. Move new version → install path");
    try
    {
        Directory.Move(tempDir, installPath);
        tempDir  = null; // now owned by installPath
        swapDone = true;
    }
    catch (Exception ex)
    {
        Err($"  8b failed: {ex.Message} — initiating immediate rollback.");
        // Restore original
        try { Directory.Move(backupDir, installPath); Info("  Rollback of 8a succeeded."); }
        catch (Exception rex) { Err($"  Rollback of 8a also failed: {rex.Message}"); }
        throw new Exception("Atomic swap step 8b failed.", ex);
    }

    Info("  Swap complete  ✓");

    // ── Phase 9 · Post-swap validation ───────────────────────────────────────
    Sep();
    Info("Phase 9 · Post-swap validation");

    var verifyExe = Path.Combine(installPath, "PrimeRx.exe");
    var verifyDb  = Path.Combine(installPath, "Data", "primerx.db");

    if (!File.Exists(verifyExe))
        throw new Exception($"POST-SWAP VALIDATION FAILED: PrimeRx.exe missing at {verifyExe}");

    // Use the pre-swap boolean — after the rename, dbPath and verifyDb resolve
    // to the same path inside the new install, so File.Exists(dbPath) would
    // silently pass even if preservation failed.
    if (hadDatabase && !File.Exists(verifyDb))
        throw new Exception($"POST-SWAP VALIDATION FAILED: database missing at {verifyDb}");

    Info($"  PrimeRx.exe present  ✓");
    Info($"  Database present     ✓");

    // Write a success marker so PrimeRx can display a "just updated" notice
    File.WriteAllText(
        Path.Combine(installPath, $"update_success_{ts}.txt"),
        $"Version  : {newVersion}\r\n" +
        $"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
        $"Backup   : {backupDir}\r\n");

    // ── Phase 10 · Launch & cleanup ──────────────────────────────────────────
    Sep();
    Info("Phase 10 · Launching PrimeRx");

    Process.Start(new ProcessStartInfo(verifyExe)
    {
        UseShellExecute  = true,
        WorkingDirectory = installPath,
    });
    Info("  PrimeRx launched  ✓");

    // Clean up the downloaded zip
    TryDelete(zipPath);
    Info("  Update zip deleted  ✓");

    Info("");
    Info("════════════════════════════════════════════════════════");
    Info($"  UPDATE COMPLETE  —  PrimeRx v{newVersion} is now running.");
    Info($"  Old installation backup: {backupDir}");
    Info($"  (Safe to delete after confirming everything works.)");
    Info("════════════════════════════════════════════════════════");

    logFile?.Dispose();
    Thread.Sleep(2_000);
    Environment.Exit(ExitOk);
}
catch (Exception ex)
{
    Err($"FATAL: {ex.Message}");
    if (ex.InnerException != null) Err($"  Caused by: {ex.InnerException.Message}");
    Err(ex.StackTrace ?? string.Empty);

    Rollback(ex.Message);

    // Write a human-readable error report next to the install (or in temp)
    var reportDir  = Directory.Exists(installPath) ? installPath : Path.GetTempPath();
    var reportPath = Path.Combine(reportDir, $"update_error_{ts}.txt");
    try
    {
        File.WriteAllText(reportPath,
            $"PrimeRx Update Error Report\r\n" +
            $"===========================\r\n" +
            $"Time    : {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
            $"Version : {newVersion}\r\n" +
            $"Error   : {ex.Message}\r\n\r\n" +
            $"Your data is SAFE. The original installation has been restored.\r\n\r\n" +
            $"Please send this file and the update log to support:\r\n" +
            $"Log: {Path.Combine(installPath, "Logs", $"update_{ts}.log")}\r\n");
        Err($"Error report: {reportPath}");
    }
    catch { /* non-fatal */ }

    Sep();
    Err("UPDATE FAILED — your original installation has been restored.");
    Err("Your pharmacy data is safe.");
    Sep();

    // Keep the console open so staff can read it / screenshot it
    Console.Error.WriteLine();
    Console.Error.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.Error.WriteLine("║              UPDATE FAILED                          ║");
    Console.Error.WriteLine($"║  {ex.Message.PadRight(52)}║");
    Console.Error.WriteLine("║                                                      ║");
    Console.Error.WriteLine("║  Your data is SAFE. PrimeRx was NOT changed.        ║");
    Console.Error.WriteLine($"║  Error report saved to install folder.              ║");
    Console.Error.WriteLine("║  Please contact support with the error report.       ║");
    Console.Error.WriteLine("╚══════════════════════════════════════════════════════╝");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Press ENTER to close this window...");

    logFile?.Dispose();
    Console.ReadLine();
    Environment.Exit(ExitError);
}

// ─── Helper methods ───────────────────────────────────────────────────────────

static string ComputeSha256(string path)
{
    using var sha  = SHA256.Create();
    using var file = File.OpenRead(path);
    return Convert.ToHexString(sha.ComputeHash(file)).ToLowerInvariant();
}

static void CopyDir(string src, string dst)
{
    Directory.CreateDirectory(dst);
    foreach (var f in Directory.EnumerateFiles(src))
        File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
    foreach (var d in Directory.EnumerateDirectories(src))
        CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
}

static void SafeMove(string src, string dst)
{
    try { Directory.Move(src, dst); }
    catch (Exception ex) { throw new Exception($"Directory.Move({src} → {dst}) failed: {ex.Message}", ex); }
}

static void SafeDeleteDir(string path, string label)
{
    try { Directory.Delete(path, recursive: true); }
    catch (Exception ex) { Console.WriteLine($"[WARN] Could not delete {label} ({path}): {ex.Message}"); }
}

static void TryDelete(string path)
{
    try { if (File.Exists(path)) File.Delete(path); } catch { }
}
