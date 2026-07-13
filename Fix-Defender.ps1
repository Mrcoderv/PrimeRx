#Requires -RunAsAdministrator
# ─────────────────────────────────────────────────────────────────────────────
#  Fix-Defender.ps1 — Add Windows Defender exclusions for PrimeRx
#
#  Run this script as Administrator on the target machine to prevent
#  Windows Defender SmartScreen / Real-time protection / Controlled
#  Folder Access from blocking PrimeRx or its SQLite database writes.
#
#  Usage:
#    Right-click PowerShell → Run as Administrator
#    .\Fix-Defender.ps1
# ─────────────────────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PrimeRx — Windows Defender Fix"          -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Detect PrimeRx folder ───────────────────────────────────────────────────
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$PrimeRxDir = Join-Path $ScriptDir "publish\win-x64"

if (-not (Test-Path $PrimeRxDir)) {
    # Fallback: assume script is in repo root, look for publish folder
    $PrimeRxDir = Join-Path $ScriptDir "publish\win-x64"
    if (-not (Test-Path $PrimeRxDir)) {
        Write-Host "  Could not find publish\win-x64 folder." -ForegroundColor Yellow
        Write-Host "  Using script directory as exclusion target." -ForegroundColor Yellow
        $PrimeRxDir = $ScriptDir
    }
}

Write-Host "  PrimeRx folder: $PrimeRxDir" -ForegroundColor White
Write-Host ""

# ── Step 1: Add folder exclusion ────────────────────────────────────────────
Write-Host "[1/3] Adding folder exclusion..." -ForegroundColor Yellow
try {
    Add-MpPreference -ExclusionPath $PrimeRxDir
    Write-Host "  OK — folder exclusion added" -ForegroundColor Green
}
catch {
    Write-Host "  SKIP — folder exclusion may already exist" -ForegroundColor DarkYellow
}

# ── Step 2: Add process exclusion for PrimeRx.exe ───────────────────────────
Write-Host "[2/3] Adding process exclusion..." -ForegroundColor Yellow
$PrimeRxExe = Join-Path $PrimeRxDir "PrimeRx.exe"
if (Test-Path $PrimeRxExe) {
    try {
        Add-MpPreference -ExclusionProcess $PrimeRxExe
        Write-Host "  OK — PrimeRx.exe process excluded" -ForegroundColor Green
    }
    catch {
        Write-Host "  SKIP — process exclusion may already exist" -ForegroundColor DarkYellow
    }
}
else {
    Write-Host "  SKIP — PrimeRx.exe not found at $PrimeRxExe" -ForegroundColor DarkYellow
}

# ── Step 3: Add exclusion for PrimeRxUpdater.exe ────────────────────────────
$UpdaterExe = Join-Path $PrimeRxDir "PrimeRxUpdater.exe"
if (Test-Path $UpdaterExe) {
    try {
        Add-MpPreference -ExclusionProcess $UpdaterExe
        Write-Host "  OK — PrimeRxUpdater.exe process excluded" -ForegroundColor Green
    }
    catch {
        Write-Host "  SKIP — updater exclusion may already exist" -ForegroundColor DarkYellow
    }
}

# ── Summary ─────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Done! Defender exclusions configured."    -ForegroundColor Green
Write-Host "══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  You can now run PrimeRx.exe without interference." -ForegroundColor White
Write-Host "  If SmartScreen still appears, click"              -ForegroundColor White
Write-Host '  "More info" → "Run anyway".'                      -ForegroundColor White
Write-Host ""
