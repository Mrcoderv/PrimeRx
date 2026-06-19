@echo off
title PrimeRx Pharmacy Billing
echo Starting PrimeRx...
set ASPNETCORE_URLS=http://localhost:5000
set ASPNETCORE_ENVIRONMENT=Production
start http://localhost:5000
WebApplication1.exe
