# ─────────────────────────────────────────────────────────────────────────────
#  PrimeRx Build & Release Script  v2.0
#  Builds PrimeRx + PrimeRxUpdater, creates a versioned zip, and generates a
#  SHA-256 checksum file so the auto-updater can verify downloads.
# ─────────────────────────────────────────────────────────────────────────────

#Requires -Version 5.1
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ── Configuration ─────────────────────────────────────────────────────────────
$OutputDir  = "publish"
$RepoOwner  = "Mrcoderv"
$RepoName   = "PrimeRx-Releases"

# ── Auto-detect version from PrimeRx.csproj ──────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PrimeRx Build Script  v2.0"              -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$CsprojPath = "PrimeRx\PrimeRx.csproj"
[xml]$Csproj = Get-Content $CsprojPath
$Version = $Csproj.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "ERROR: Could not read <Version> from $CsprojPath" -ForegroundColor Red
    exit 1
}
Write-Host "  Version detected: $Version" -ForegroundColor Green
Write-Host ""

$ZipName  = "PrimeRx-win-x64-v$Version.zip"
$ZipPath  = "$OutputDir\$ZipName"
$Sha256Path = "$ZipPath.sha256"

# ── Step 1 · Build PrimeRx ────────────────────────────────────────────────────
Write-Host "Step 1 · Building PrimeRx…" -ForegroundColor Yellow

dotnet publish PrimeRx\PrimeRx.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o "$OutputDir\win-x64" `
    /p:PublishSingleFile=false   # keep DLLs separate for easier diffing

if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAILED — build error above." -ForegroundColor Red
    exit 1
}
Write-Host "  PrimeRx built  ✓" -ForegroundColor Green
Write-Host ""

# ── Step 2 · Build PrimeRxUpdater ─────────────────────────────────────────────
Write-Host "Step 2 · Building PrimeRxUpdater…" -ForegroundColor Yellow

dotnet publish PrimeRxUpdater\PrimeRxUpdater.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o "$OutputDir\updater"

if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAILED — build error above." -ForegroundColor Red
    exit 1
}
Write-Host "  PrimeRxUpdater built  ✓" -ForegroundColor Green
Write-Host ""

# ── Step 3 · Copy updater into PrimeRx publish dir ───────────────────────────
Write-Host "Step 3 · Bundling updater with PrimeRx…" -ForegroundColor Yellow

$UpdaterExe = "$OutputDir\updater\PrimeRxUpdater.exe"
if (-not (Test-Path $UpdaterExe)) {
    Write-Host "  FAILED — PrimeRxUpdater.exe not found at $UpdaterExe" -ForegroundColor Red
    exit 1
}
Copy-Item $UpdaterExe "$OutputDir\win-x64\PrimeRxUpdater.exe" -Force
Write-Host "  PrimeRxUpdater.exe copied  ✓" -ForegroundColor Green
Write-Host ""

# ── Step 4 · Create release zip ───────────────────────────────────────────────
Write-Host "Step 4 · Creating release zip…" -ForegroundColor Yellow

if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$OutputDir\win-x64\*" -DestinationPath $ZipPath

$ZipSize = (Get-Item $ZipPath).Length / 1MB
Write-Host ("  {0}  ({1:F2} MB)  ✓" -f $ZipName, $ZipSize) -ForegroundColor Green
Write-Host ""

# ── Step 5 · Generate SHA-256 checksum ───────────────────────────────────────
Write-Host "Step 5 · Computing SHA-256 checksum…" -ForegroundColor Yellow

$Sha256 = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash.ToLower()

# Format: "<hex>  <filename>"  (compatible with sha256sum on Linux/macOS)
"$Sha256  $ZipName" | Out-File -FilePath $Sha256Path -Encoding ascii -NoNewline

Write-Host "  SHA-256: $Sha256" -ForegroundColor Green
Write-Host "  Written to: $Sha256Path  ✓" -ForegroundColor Green
Write-Host ""

# ── Step 6 · Summary ─────────────────────────────────────────────────────────
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Build complete!  v$Version"               -ForegroundColor Green
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Files to upload to GitHub releases:" -ForegroundColor White
Write-Host "    $ZipPath" -ForegroundColor Cyan
Write-Host "    $Sha256Path" -ForegroundColor Cyan
Write-Host ""
Write-Host "  GitHub releases URL:" -ForegroundColor White
Write-Host "    https://github.com/$RepoOwner/$RepoName/releases/new" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Release checklist:" -ForegroundColor Yellow
Write-Host "    [ ] Create tag:  v$Version" -ForegroundColor White
Write-Host "    [ ] Upload:      $ZipName" -ForegroundColor White
Write-Host "    [ ] Upload:      $ZipName.sha256" -ForegroundColor White
Write-Host "    [ ] Add release notes (what changed in v$Version)" -ForegroundColor White
Write-Host "    [ ] Publish release" -ForegroundColor White
Write-Host ""
Write-Host "  SHA-256 (keep a copy):" -ForegroundColor Yellow
Write-Host "    $Sha256" -ForegroundColor White
Write-Host ""
