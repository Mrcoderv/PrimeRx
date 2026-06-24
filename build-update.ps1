# PrimeRx Build and Update Script
# This script builds PrimeRx and the updater for release

$ErrorActionPreference = "Stop"

$Version = "1.0.0"
$OutputDir = "publish"
$RepoOwner = "Mrcoderv"
$RepoName = "PrimeRx-Releases"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PrimeRx Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build PrimeRx
Write-Host "Step 1: Building PrimeRx..." -ForegroundColor Yellow
dotnet publish PrimeRx\PrimeRx.csproj -c Release -r win-x64 --self-contained true -o "$OutputDir\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build PrimeRx" -ForegroundColor Red
    exit 1
}

Write-Host "PrimeRx built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Build PrimeRxUpdater
Write-Host "Step 2: Building PrimeRxUpdater..." -ForegroundColor Yellow
dotnet publish PrimeRxUpdater\PrimeRxUpdater.csproj -c Release -r win-x64 --self-contained true -o "$OutputDir\updater"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build PrimeRxUpdater" -ForegroundColor Red
    exit 1
}

Write-Host "PrimeRxUpdater built successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Copy updater to PrimeRx publish directory
Write-Host "Step 3: Copying updater to PrimeRx directory..." -ForegroundColor Yellow
Copy-Item "$OutputDir\updater\PrimeRxUpdater.exe" "$OutputDir\win-x64\PrimeRxUpdater.exe" -Force

Write-Host "Updater copied successfully" -ForegroundColor Green
Write-Host ""

# Step 4: Create release zip
Write-Host "Step 4: Creating release zip..." -ForegroundColor Yellow
$ZipName = "PrimeRx-win-x64-v$Version.zip"
Compress-Archive -Path "$OutputDir\win-x64\*" -DestinationPath "$OutputDir\$ZipName" -Force

Write-Host "Release zip created: $ZipName" -ForegroundColor Green
Write-Host ""

# Step 5: Display instructions
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Upload $OutputDir\$ZipName to GitHub releases" -ForegroundColor White
Write-Host "2. Create a new release with tag: v$Version" -ForegroundColor White
Write-Host "3. Repository: https://github.com/$RepoOwner/$RepoName/releases" -ForegroundColor White
Write-Host ""
Write-Host "File location: $OutputDir\$ZipName" -ForegroundColor Cyan
Write-Host ""
