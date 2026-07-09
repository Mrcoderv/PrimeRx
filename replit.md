# PrimeRx

**PrimeRx** is a professional pharmacy billing, inventory, and purchase management system built with ASP.NET Core Razor Pages and SQLite.

## Stack

- **Framework:** .NET 10 / ASP.NET Core Razor Pages
- **Database:** SQLite (`PrimeRx/Data/primerx.db`)
- **Auth:** ASP.NET Core Identity (roles: Admin, Staff)
- **PDF:** QuestPDF
- **Excel:** EPPlus
- **Logging:** Serilog

## How to run

The workflow **"Start application"** runs the app:

```
cd PrimeRx && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet run --no-launch-profile
```

The app is available at port **5000**.

## Key modules

| Module | Pages location |
|--------|---------------|
| Dashboard | `Pages/Dashboard` |
| Billing (POS) | `Pages/Billing` |
| Purchase Entry | `Pages/Purchase` |
| Inventory | `Pages/Inventory` |
| Due Payments | `Pages/Due` |
| Reports | `Pages/Reports` |
| Admin | `Areas/Admin` |

## Configuration

| Setting | Location |
|---------|----------|
| DB path | `appsettings.json` → `ConnectionStrings.DefaultConnection` |
| Listen URL | `ASPNETCORE_URLS` env var |
| Environment | `ASPNETCORE_ENVIRONMENT` env var |

## User preferences

<!-- Add user preferences here -->
