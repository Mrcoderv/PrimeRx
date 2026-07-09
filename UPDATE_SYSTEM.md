# PrimeRx Auto-Update System — Safety Guide  v2.0

> **Design principle:** For a pharmacy management system, data loss is unacceptable.
> Every step of the update pipeline is designed so that a failure at any point
> leaves the original installation completely intact.

---

## Architecture Overview

```
PrimeRx (web app)                 PrimeRxUpdater (console exe)
─────────────────                 ────────────────────────────
1. Check GitHub API            →  (spawned by PrimeRx)
2. Download zip + checksum     →
3. Verify SHA-256              →
4. Launch PrimeRxUpdater            5. Wait for PrimeRx to exit
5. Environment.Exit(0)         →    6. Re-verify SHA-256
                                    7. Extract to Temp_<GUID>/
                                    8. Validate extracted package
                                    9. Backup DB + config
                                   10. Preserve user data → temp
                                   11. ATOMIC SWAP (rename×2)
                                   12. Post-swap validation
                                   13. Launch new PrimeRx.exe
                                   14. Rollback on any failure
```

### Components

| Component | File | Role |
|-----------|------|------|
| **UpdateService** | `PrimeRx/Services/UpdateService.cs` | GitHub API, download, SHA-256 |
| **PrimeRxUpdater** | `PrimeRxUpdater/Program.cs` | Atomic swap + rollback |
| **Settings UI** | `Pages/Admin/Settings/Index.cshtml.cs` | Admin-facing trigger |
| **Build Script** | `build-update.ps1` | Build + checksum generation |

### Repositories

| Repo | Visibility | Purpose |
|------|-----------|---------|
| `Mrcoderv/PrimeRx` | Private | Source code |
| `Mrcoderv/PrimeRx-Releases` | Public | Release zips + checksums |

---

## Safety Features

### 1 · SHA-256 Checksum Verification (double-checked)

The build script generates a `.sha256` file alongside every release zip:

```
PrimeRx-win-x64-v1.2.0.zip
PrimeRx-win-x64-v1.2.0.zip.sha256   ← contains lowercase hex hash
```

`UpdateService.PrepareUpdateAsync()` downloads both files and verifies the
hash **before** the updater is launched. `PrimeRxUpdater.exe` then
**re-verifies** the hash as its first act, so a corrupt zip can never reach
the extraction step.

If the hashes do not match:
- The corrupt zip is deleted immediately.
- The update is cancelled with a clear error message.
- The original installation is **not touched**.

---

### 2 · Atomic Directory Swap

Replacing a running application safely on Windows requires avoiding in-place
file overwrites. PrimeRxUpdater uses a **rename-rename swap** on the same NTFS
volume, which is atomic at the file-system level:

```
Phase 8a:  rename  C:\PrimeRx\            →  C:\PrimeRx_OldInstall_20260709_120000\
Phase 8b:  rename  C:\PrimeRx_Temp_<GUID>\ →  C:\PrimeRx\
```

`Directory.Move()` within the same drive is a metadata-only operation — no
bytes are copied. There is no window during which the install directory is
absent or partially written.

If Phase 8b fails (extremely rare), Phase 8a is immediately reversed:
`C:\PrimeRx_OldInstall_…\` is renamed back to `C:\PrimeRx\`.

---

### 3 · Automatic Rollback

| Failure point | Rollback action |
|---------------|-----------------|
| Bad checksum | Zip deleted, update cancelled |
| Extraction fails | Temp dir deleted, install untouched |
| Package validation fails | Temp dir deleted, install untouched |
| Backup fails | Temp dir deleted, install untouched |
| Phase 8a (rename) fails | Temp dir deleted, install untouched |
| Phase 8b (rename) fails | Phase 8a reversed, temp dir deleted |
| Post-swap validation fails | New install moved to `_FAILED_…`, backup restored |

After any rollback, `PrimeRxUpdater` displays a prominent error box and
writes an error report to the install folder so staff can send it to support.

---

### 4 · User Data Preservation

Before the atomic swap, the updater copies the following from the **current**
installation into the **new** (temp) directory:

| Path | Why it's preserved |
|------|--------------------|
| `Data/primerx.db` | The live database — never overwritten |
| `Data/primerx.db-wal` | SQLite write-ahead log |
| `Data/primerx.db-shm` | SQLite shared memory |
| `Backups/` | All prior backups (including the pre-update one) |
| `wwwroot/uploads/` | Company logos, uploaded images |
| `appsettings.json` | Connection string and admin config |
| `Logs/` | Operational log history |

---

### 5 · Pre-Update Database Backup

Before touching anything, PrimeRxUpdater writes a timestamped backup:

```
<InstallDir>/
└── Backups/
    └── pre_update_20260709_120000/
        ├── primerx.db          ← full database copy
        ├── primerx.db.sha256   ← hash for verification
        └── appsettings.json    ← config snapshot
```

This backup survives the update (it's inside `Backups/` which is copied to
the new install). It can be restored via **Admin → Backup & Restore**.

---

### 6 · Old Installation Backup

After a successful update the old installation is retained as a sibling
directory until you delete it manually:

```
C:\
├── PrimeRx\                        ← new version (running)
└── PrimeRx_OldInstall_20260709_120000\   ← previous version (safe to delete)
```

Confirm the new version works for a day or two, then delete the old folder.
Disk usage is roughly double during this period (typically ~100–150 MB).

---

### 7 · Post-Swap Validation

After the swap, the updater checks:
- `PrimeRx.exe` is present and accessible.
- `Data/primerx.db` is present (if it existed before the update).

If either check fails, the update is rolled back and the old installation is
restored automatically.

---

## Update Process (step by step)

1. Admin navigates to **Admin → Settings → Updates**.
2. Clicks **Check for Updates** — PrimeRx contacts the GitHub releases API.
3. If a newer version exists, clicks **Update Now**.
4. PrimeRx downloads the zip and checksum from GitHub.
5. SHA-256 is verified — if it fails, the process stops here.
6. `PrimeRxUpdater.exe` is launched in a visible console window.
7. PrimeRx calls `Environment.Exit(0)` — the web server stops.
8. The updater window shows live progress through all phases.
9. On success, the new PrimeRx starts automatically.
10. On failure, the console shows a clear error and the old install is restored.

---

## Building a Release

```powershell
.\build-update.ps1
```

This will:
1. Read `<Version>` from `PrimeRx/PrimeRx.csproj` automatically.
2. Publish PrimeRx for win-x64 (self-contained).
3. Publish PrimeRxUpdater for win-x64 (self-contained single exe).
4. Bundle both into a zip: `publish\PrimeRx-win-x64-vX.Y.Z.zip`.
5. Compute SHA-256 and write `publish\PrimeRx-win-x64-vX.Y.Z.zip.sha256`.

**Both files must be uploaded to the GitHub release.**

---

## Creating a GitHub Release

1. Go to: `https://github.com/Mrcoderv/PrimeRx-Releases/releases/new`
2. Set tag: `vX.Y.Z` (must match `<Version>` in PrimeRx.csproj)
3. Set title: `PrimeRx vX.Y.Z`
4. Write release notes (what changed, bug fixes, etc.)
5. Upload **both** files:
   - `PrimeRx-win-x64-vX.Y.Z.zip`
   - `PrimeRx-win-x64-vX.Y.Z.zip.sha256`
6. Publish the release.

> ⚠️ If you publish without the `.sha256` file, the updater will still work
> but will skip checksum verification and log a warning.

---

## Version Management

Version is the single source of truth in `PrimeRx/PrimeRx.csproj`:

```xml
<PropertyGroup>
    <Version>1.2.0</Version>
</PropertyGroup>
```

The build script reads it automatically. Update it here before running the
build; no other files need changing.

---

## File Structure

```
<InstallDir>/
├── PrimeRx.exe                   Main application
├── PrimeRxUpdater.exe            Safe updater (bundled in release zip)
├── appsettings.json              Config (preserved across updates)
├── Data/
│   └── primerx.db               Database (NEVER replaced by update)
├── Backups/
│   ├── pre_update_20260709_.../  Pre-update snapshot (auto-created)
│   ├── primerx_backup_20260708_120000.db
│   └── …
├── wwwroot/
│   └── uploads/                 User-uploaded files (logos, etc.)
├── Logs/
│   ├── primerx-20260709.log     App logs
│   └── update_20260709_120000.log  Updater log (created per update)
└── update_success_<ts>.txt      Written after a successful update
```

---

## PrimeRxUpdater Command Line

```
PrimeRxUpdater.exe <zipPath> <installPath> [options]

Options:
  --sha256 <hex>    Expected SHA-256 hash of the zip (re-verified by updater)
  --pid    <int>    PID of the main PrimeRx process to wait for
  --version <str>   New version label for log messages

Exit codes:
  0   Success
  1   Failure (original installation restored)
```

---

## UpdateService API

```csharp
// Check GitHub for a newer release
UpdateInfo info = await updateService.CheckForUpdatesAsync();

// Download + verify (throws InvalidOperationException on checksum mismatch)
UpdatePackage pkg = await updateService.PrepareUpdateAsync(
    info,
    destinationZipPath,
    progress: new Progress<int>(pct => Console.WriteLine($"{pct}%")));

// info.ChecksumUrl        — URL of the .sha256 asset (null if not published)
// pkg.ChecksumVerified    — true if hash was present and matched
// pkg.VerifiedChecksum    — the SHA-256 hex string used for verification
```

---

## Troubleshooting

### "Update not available"
- Check internet connectivity on the server.
- Confirm the GitHub release is **published** (not draft).
- Confirm both files (`zip` + `zip.sha256`) are attached.
- Confirm the release tag matches the version format (`v1.2.0`).

### "Checksum mismatch"
- The downloaded zip was corrupted in transit — retry.
- If it keeps failing, download the zip manually and verify:
  ```powershell
  Get-FileHash .\PrimeRx-win-x64-v1.2.0.zip -Algorithm SHA256
  ```
  Compare to the contents of the `.sha256` file.

### "PrimeRxUpdater.exe not found"
- It must be in the same folder as `PrimeRx.exe`.
- Reinstall from the latest release zip (which includes the updater).

### Update failed — how to recover manually
1. Check the updater log: `Logs\update_<timestamp>.log`.
2. Look for a sibling directory named `PrimeRx_OldInstall_<timestamp>`.
3. If found, copy its contents back into the install directory.
4. The database is also backed up in `Backups\pre_update_<timestamp>\`.

### Database appears empty after update
1. Open `Backups\pre_update_<timestamp>\primerx.db`.
2. Copy it to `Data\primerx.db`.
3. Verify the SHA-256 matches `Backups\pre_update_<timestamp>\primerx.db.sha256`.

---

## Security Considerations

| Concern | Mitigation |
|---------|-----------|
| Tampered release zip | SHA-256 double-checked (download + updater) |
| Corrupted download | SHA-256 mismatch → update cancelled, zip deleted |
| Partial extraction | Package validation (exe presence + size + file count) |
| Data loss during swap | Atomic rename — no window where install is missing |
| Failed swap | Automatic rollback from pre-swap backup |
| Source code exposure | Main repo remains private; only compiled binaries released |
