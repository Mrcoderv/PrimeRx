# PrimeRx

**PrimeRx** is a professional pharmacy billing, inventory, and purchase management system built for commercial pharmacy operations. It provides fast point-of-sale billing, real-time stock control, purchase entry with batch/expiry tracking, due payment tracking, analytics dashboards, and exportable reports.

**Developed by Prime LogicTech**  
📞 986-7788298 | 📧 primelogictech3@gmail.com  
🌐 [https://www.facebook.com/PrimeLogictech](https://www.facebook.com/PrimeLogictech)

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Razor%20Pages-512BD4)
![SQLite](https://img.shields.io/badge/Database-SQLite-003B57)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D4)

---

## Features

| Module | Capabilities |
|--------|----------------|
| **Dashboard** | Today's sales, monthly revenue, due outstanding, stock alerts, 7-day sales chart, top medicines |
| **Billing (POS)** | Medicine autocomplete popup (purchase-style), multi-item bills, discounts, smart Enter key navigation, floating calculator, Cash / Online / Due payments, PDF invoices |
| **Purchase Entry** | Multi-item purchase recording with **CC (Conversion Charges)**, batch number & expiry tracking, smart Enter key navigation (dakadak), floating calculator, batch info panel, medicine master auto-fill, per-item discount, MRP auto-calculation, supplier management, credit purchase with auto-payable |
| **Inventory** | Stock view, add medicines (Staff & Admin), batch management, expiry tracking, stock adjustments, transaction history |
| **Due Payments** | Search customers, partial/full collection, payment history |
| **Ageing Dues** | Unified supplier payable & customer receivable ageing report with date filters, age color-coding, separate PDF exports |
| **Reports** | Sales bar charts, medicine pie charts, daily/monthly reports, P&L, inventory & expiry (PDF/Excel), supplier profit |
| **Admin** | Company profile, staff account management, medicine master catalog, supplier management, full medicine catalog control |

### User Roles

- **Admin** — Full access including company setup, staff management, medicine master catalog, and supplier management
- **Staff** — Billing, purchase entry, inventory (including add medicine), due collection, reports, dashboard

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

> **Database:** SQLite file at `Data\primerx.db` — back up this file regularly to preserve all business data.

### Windows Defender (New Device)

On a new device, Windows Defender may block PrimeRx because the `.exe` is unsigned. Run the included fix script **as Administrator**:

```powershell
# Right-click PowerShell → Run as Administrator
.\Fix-Defender.ps1
```

This adds folder and process exclusions so Defender won't interfere with PrimeRx or its database.

If you prefer to fix it manually:
1. Open **Windows Security** → **Virus & threat protection** → **Manage settings**
2. Scroll to **Exclusions** → **Add or remove exclusions**
3. Add exclusion → **Folder** → select `publish\win-x64`
4. If **Controlled Folder Access** is enabled, add `PrimeRx.exe` as an allowed app

If SmartScreen still appears when launching, click **"More info"** → **"Run anyway"**.

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
├── PrimeRx/                  # ASP.NET Core Razor Pages application
│   ├── Data/                 # DbContext, migrations, seeders
│   ├── Models/               # Domain entities & view models
│   ├── Services/             # Billing, purchase, inventory, reports, dashboard
│   ├── Helpers/              # PDF invoice generator (QuestPDF), number-to-words
│   ├── Middleware/           # First-run setup redirect
│   ├── Pages/                # Razor UI (Billing, Purchase, Dashboard, Reports…)
│   │   ├── Billing/          # Point of Sale (POS)
│   │   ├── Purchase/         # Purchase entry, edit, history, returns
│   │   ├── Inventory/        # Stock management, batches, expiry
│   │   ├── Dashboard/        # Analytics KPIs and charts
│   │   ├── Reports/          # Sales, inventory, profit reports
│   │   └── ...
│   └── wwwroot/              # CSS, JavaScript, static assets
│       ├── css/
│       │   ├── site.css      # Design system (light/dark themes)
│       │   ├── billing.css   # Billing & POS styles
│       │   └── purchase.css  # Purchase entry styles
│       └── js/
│           ├── billing.js    # POS client-side logic
│           └── purchase.js   # Purchase entry client-side logic
├── publish/win-x64/          # Ready-to-run self-contained release
├── CODE_MAP.md               # Full code map: methods, classes, workflows
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
| UI | Bootstrap 5, custom professional dark/light theme |

---

## Billing & Purchase Workflows

### Billing (Point of Sale)
1. Search and add medicines via autocomplete popup (purchase-style layout)
2. Smart Enter key navigation: Rate → Qty → Disc% (rapid data entry)
3. Enter customer name, phone, payment method (Cash / Online / Due)
4. Apply optional discount per line item
5. Generate bill → stock deducted automatically → PDF invoice available

### Purchase Entry
1. Select supplier (or type a new one)
2. Search and add medicines via autocomplete popup
3. Smart Enter key navigation: Batch# → Expiry → Qty → Free → Rate → Disc% → CC → MRP
4. Per-item CC (Conversion Charges) tracked
5. MRP auto-calculated from configured margin percent
6. Floating calculator (right-click or Ctrl+Shift+C on Qty/Rate/CC fields)
7. Batch info panel showing medicine details on focus
8. Total section: Subtotal, Discount, Total CC, Net Amount
9. Save → stock updated, batch records created, payable auto-created for credit purchases

---

---

## Keyboard Shortcuts

### Global

| Shortcut | Action |
|----------|--------|
| <kbd>Ctrl</kbd>+<kbd>K</kbd> | Open/Close global feature search |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> | Open floating calculator on active input |
| <kbd>Alt</kbd>+<kbd>C</kbd> (calculator open) | Insert calculator value into active field |
| <kbd>Esc</kbd> | Close popups / search overlay / help modal |

### Ageing Dues Page

| Shortcut | Action |
|----------|--------|
| <kbd>F1</kbd> | Show keyboard shortcut help overlay |
| <kbd>Alt</kbd>+<kbd>1</kbd> | Generate Supplier PDF |
| <kbd>Alt</kbd>+<kbd>2</kbd> | Generate Customer PDF |
| <kbd>Alt</kbd>+<kbd>F</kbd> | Focus Party filter dropdown |
| <kbd>Alt</kbd>+<kbd>C</kbd> | Clear all filters |
| <kbd>Enter</kbd> (on filter form) | Apply filters |

### Billing Page (POS)

| Shortcut | Action |
|----------|--------|
| <kbd>Enter</kbd> (Rate input) | Move to Qty field |
| <kbd>Enter</kbd> (Qty input) | Move to Disc% field |
| <kbd>Enter</kbd> (Disc% input) | Move to search bar |
| <kbd>1</kbd>-<kbd>9</kbd> | Quick-set quantity on last added item |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> / Right-click | Open floating calculator on Rate/Qty/Disc fields |
| <kbd>↑</kbd><kbd>↓</kbd> (popup open) | Navigate search results |
| <kbd>Enter</kbd> (popup open) | Select highlighted medicine |
| <kbd>Esc</kbd> (popup open) | Close medicine search popup |

### Purchase Entry Page

| Shortcut | Action |
|----------|--------|
| <kbd>Enter</kbd> (consecutive fields) | Batch# → Expiry → Qty → Free → Rate → Disc% → CC → MRP → next search |
| <kbd>↑</kbd><kbd>↓</kbd> (table) | Navigate between rows in same column |
| <kbd>↑</kbd><kbd>↓</kbd> (popup open) | Navigate search results |
| <kbd>Enter</kbd> (popup open) | Select highlighted medicine |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> / Right-click | Open floating calculator on Qty/Rate/CC fields |
| <kbd>Esc</kbd> (popup open) | Close medicine search popup |

### Calculator (Floating)

| Shortcut | Action |
|----------|--------|
| <kbd>0</kbd>-<kbd>9</kbd>, <kbd>.</kbd> | Input numbers/decimal |
| <kbd>+</kbd>, <kbd>-</kbd>, <kbd>*</kbd>, <kbd>/</kbd>, <kbd>%</kbd> | Operators |
| <kbd>Enter</kbd> / <kbd>=</kbd> | Evaluate expression |
| <kbd>Backspace</kbd> | Delete last character |
| <kbd>Esc</kbd> / <kbd>C</kbd> | Clear display |
| <kbd>U</kbd> | Insert value into active field |
| <kbd>Alt</kbd>+<kbd>C</kbd> | Insert value (when typing in another input) |

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
- On a new device, run `Fix-Defender.ps1` as Administrator to prevent Windows Defender interference
- For production, set a fixed port and consider running as a Windows Service or behind IIS
- Back up `Data\primerx.db` daily

---

## Repository

**GitHub:** [https://github.com/Mrcoderv/PrimeRx](https://github.com/Mrcoderv/PrimeRx)

---

## License

This project is intended for commercial pharmacy use. Contact the repository owner for licensing terms.

---

## Author & Support

**Developed by Prime LogicTech**  
📞 986-7788298 | 📧 primelogictech3@gmail.com  
🌐 [https://www.facebook.com/PrimeLogictech](https://www.facebook.com/PrimeLogictech)

**GitHub:** [https://github.com/Mrcoderv/PrimeRx](https://github.com/Mrcoderv/PrimeRx)
