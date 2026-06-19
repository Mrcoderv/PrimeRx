@echo off
title PrimeRx Pharmacy Billing
cd /d "%~dp0"
echo Starting PrimeRx...
set ASPNETCORE_URLS=http://localhost:5000
set ASPNETCORE_ENVIRONMENT=Production
PrimeRx.exe
if errorlevel 1 pause
