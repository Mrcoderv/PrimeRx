# PrimeRx

**PrimeRx** is a professional pharmacy billing and inventory management system built for commercial pharmacy operations. It provides fast point-of-sale billing, real-time stock control, due payment tracking, analytics dashboards, and exportable reports.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Razor%20Pages-512BD4)
![SQLite](https://img.shields.io/badge/Database-SQLite-003B57)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D4)

---

## Features

| Module | Capabilities |
|--------|----------------|
| **Dashboard** | Today's sales, monthly revenue, due outstanding, stock alerts, 7-day sales chart, top medicines |
| **Billing** | Medicine autocomplete, multi-item bills, discounts, Cash / Online / Due payments, PDF invoices |
| **Inventory** | Stock view, add medicines (Staff & Admin), purchase entry, adjustments, transaction history |
| **Due Payments** | Search customers, partial/full collection, payment history |
| **Reports** | Sales bar charts, medicine pie charts, daily/monthly reports, P&L, inventory & expiry (PDF/Excel) |
| **Admin** | Company profile, staff account management, full medicine catalog control |

### User Roles

- **Admin** — Full access including company setup, staff management, and medicine editing
- **Staff** — Billing, inventory (including add medicine), due collection, reports, dashboard

---

## Quick Start (Published Build)

A **self-contained Windows x64** release is included — no .NET installation required.

```powershell
cd publish\win-x64
$env:ASPNETCORE_URLS="http://localhost:5000"
.\PrimeRx.exe
```

Open **http://localhost:5000** in your browser.

### First-Time Setup

1. Complete the **Setup** wizard (pharmacy name, address, phone, admin account)
2. Optionally load the sample medicine database
3. Log in and start from the **Dashboard**

> **Database:** SQLite file at `publish\win-x64\Data\primerx.db` — back up this file regularly to preserve all business data.

---

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows (recommended) or any OS for development

### Run from source

```powershell
cd PrimeRx
dotnet restore
dotnet run
```

Default URL: `https://localhost:7261` or `http://localhost:5181` (see `Properties/launchSettings.json`).

### Database

Development uses SQLite:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=Data/primerx.db"
}
```

Migrations run automatically on startup.

### Create a new self-contained publish

```powershell
dotnet publish PrimeRx\PrimeRx.csproj `
  -c Release -r win-x64 --self-contained true `
  -o publish\win-x64
```

---

## Project Structure

```
PrimeRx/
├── PrimeRx/          # ASP.NET Core Razor Pages application
│   ├── Data/                   # DbContext, migrations, seeders
│   ├── Models/                 # Domain entities & view models
│   ├── Services/               # Billing, inventory, reports, dashboard
│   ├── Helpers/                # PDF invoice generator (QuestPDF)
│   ├── Pages/                  # Razor UI (Billing, Dashboard, Reports…)
│   ├── ViewComponents/         # Company header banner
│   └── wwwroot/                # CSS, JavaScript, static assets
├── publish/win-x64/            # Ready-to-run self-contained release
├── CODE_MAP.md                 # Full code map: methods, classes, workflows
└── README.md
```

> **Developer reference:** See [CODE_MAP.md](CODE_MAP.md) for a complete listing of every service method, page handler, model, and workflow.

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 10, Razor Pages |
| Database | SQLite (embedded, portable) |
| ORM | Entity Framework Core |
| Auth | ASP.NET Core Identity (Admin / Staff roles) |
| PDF Invoices | QuestPDF |
| Excel Reports | EPPlus |
| Charts | Chart.js |
| Medicine Search | Tom Select |
| UI | Bootstrap 5, custom professional blue theme |

---

## Billing Workflow

1. Search and add medicines
2. Enter customer name and phone
3. Select payment method (Cash / Online / Due)
4. Apply optional discount
5. Generate bill → stock deducted automatically → PDF invoice available

---

## Configuration

| Setting | Location | Description |
|---------|----------|-------------|
| Database path | `appsettings.json` | `Data Source=Data/primerx.db` |
| Listen URL | Environment variable | `ASPNETCORE_URLS=http://localhost:5000` |
| Environment | Environment variable | `ASPNETCORE_ENVIRONMENT=Production` |

---

## Deployment Notes

- Run `PrimeRx.exe` from the `publish\win-x64` folder
- Keep the entire folder together (runtime + `wwwroot` + `Data` are required)
- For production, set a fixed port and consider running as a Windows Service or behind IIS
- Back up `Data\primerx.db` daily

---

## Repository

**GitHub:** [https://github.com/Mrcoderv/PrimeRx](https://github.com/Mrcoderv/PrimeRx)

---

## License

This project is intended for commercial pharmacy use. Contact the repository owner for licensing terms.

---

## Author

**Mrcoderv** — [PrimeRx on GitHub](https://github.com/Mrcoderv/PrimeRx)
