# PrimeRx Auto-Update System

This document describes the auto-update system implemented for PrimeRx.

## Architecture

The auto-update system consists of three main components:

1. **UpdateService** - Checks GitHub for updates and downloads release files
2. **PrimeRxUpdater** - Console application that performs the actual update
3. **Settings UI** - Admin interface to check for and install updates

## Repository Structure

### Private Repository: `Mrcoderv/PrimeRx`
- Contains the source code
- Never exposed to customers

### Public Repository: `Mrcoderv/PrimeRx-Releases`
- Contains only release files (zipped binaries)
- Used for update checks
- Structure:
  ```
  PrimeRx-Releases/
  ├── Releases/
  └── README.md
  ```

## Version Management

Version is defined in `PrimeRx.csproj`:
```xml
<PropertyGroup>
    <Version>1.0.0</Version>
</PropertyGroup>
```

When releasing a new version:
1. Update the version number in `PrimeRx.csproj`
2. Update the version in `PrimeRxUpdater/PrimeRxUpdater.csproj`
3. Build and create release
4. Upload to GitHub releases with tag `vX.Y.Z`

## Building a Release

Use the provided PowerShell script:

```powershell
.\build-update.ps1
```

This will:
1. Build PrimeRx for win-x64 (self-contained)
2. Build PrimeRxUpdater for win-x64 (self-contained)
3. Copy PrimeRxUpdater.exe to PrimeRx publish directory
4. Create a zip file: `PrimeRx-win-x64-vX.Y.Z.zip`

## Manual Build Steps

If you prefer manual builds:

```powershell
# Build PrimeRx
dotnet publish PrimeRx\PrimeRx.csproj -c Release -r win-x64 --self-contained true -o publish\win-x64

# Build PrimeRxUpdater
dotnet publish PrimeRxUpdater\PrimeRxUpdater.csproj -c Release -r win-x64 --self-contained true -o publish\updater

# Copy updater to PrimeRx directory
Copy-Item publish\updater\PrimeRxUpdater.exe publish\win-x64\PrimeRxUpdater.exe

# Create zip
Compress-Archive -Path publish\win-x64\* -DestinationPath PrimeRx-win-x64-v1.0.0.zip
```

## Creating a GitHub Release

1. Go to: https://github.com/Mrcoderv/PrimeRx-Releases/releases
2. Click "Draft a new release"
3. Fill in:
   - Tag: `v1.0.0` (match your version)
   - Release title: `PrimeRx v1.0.0`
   - Description: Add release notes
4. Upload the zip file created above
5. Publish the release

## Update Process

### Automatic Check

PrimeRx automatically checks for updates on startup (after 5 seconds) and logs to console:

```
============================================================
UPDATE AVAILABLE
============================================================
Current Version: 1.0.0
Latest Version:  1.0.1
Download Size:   45.23 MB

Release Notes:
- Bug fixes
- New features

To update, visit: https://github.com/Mrcoderv/PrimeRx-Releases/releases/tag/v1.0.1
============================================================
```

### Manual Check via UI

1. Navigate to Admin → Settings
2. Click the "Updates" tab
3. Click "Check for Updates"
4. If an update is available, click "Update Now"

### Update Process

When user clicks "Update Now":

1. **Download**: Update zip is downloaded to temp directory
2. **Backup**: Database is backed up to `Backups/primerx_YYYYMMDDHHmmss.db`
3. **Launch Updater**: PrimeRxUpdater.exe is started with download path
4. **Shutdown**: PrimeRx closes
5. **Extract**: Updater extracts files to installation directory
6. **Cleanup**: Zip file is deleted
7. **Restart**: PrimeRx.exe is launched automatically

## Database Protection

The update system **never replaces the database**. The database file (`Data/primerx.db`) is:

1. Backed up before update
2. Preserved during extraction (overwrite is set to preserve existing files)
3. Restored automatically from backup if needed

## File Structure After Installation

```
PrimeRx/
├── PrimeRx.exe              # Main application
├── PrimeRxUpdater.exe       # Update tool
├── Data/
│   └── primerx.db          # Database (never replaced)
├── Backups/                 # Database backups
│   ├── primerx_20250123120000.db
│   └── primerx_20250123130000.db
├── wwwroot/                # Static files
└── *.dll                   # Dependencies
```

## UpdateService API

### CheckForUpdatesAsync
```csharp
var updateInfo = await updateService.CheckForUpdatesAsync();
// Returns: UpdateInfo with version comparison and download URL
```

### DownloadUpdateAsync
```csharp
var downloadedPath = await updateService.DownloadUpdateAsync(url, destinationPath);
// Downloads zip file to specified path
```

## PrimeRxUpdater Arguments

```bash
PrimeRxUpdater.exe <zipFilePath> <installPath>
```

- `zipFilePath`: Path to downloaded update zip
- `installPath`: Directory where PrimeRx is installed

## Security Considerations

1. **Private Source Code**: Main repository remains private
2. **Public Releases Only**: Only compiled binaries are public
3. **GitHub API**: Uses public GitHub API (no authentication needed)
4. **Database Safety**: Automatic backup before any update
5. **Checksum Verification**: Consider adding SHA256 verification in future

## Troubleshooting

### Update not showing
- Check internet connection
- Verify GitHub repository is public
- Check console logs for errors
- Ensure version numbers are correct

### Update fails
- Check that PrimeRxUpdater.exe exists in installation directory
- Verify disk space
- Check file permissions
- Review backup directory for database integrity

### Database issues
- Check `Backups/` directory for recent backups
- Manually restore from backup if needed
- Ensure database file is not locked during update

## Future Enhancements

1. Add SHA256 checksum verification
2. Support for multiple platforms (Linux, macOS)
3. Silent updates option
4. Rollback capability
5. Update scheduling
6. Beta/preview channel support
