# Objective
Implement Phases 1–5 of the PrimeRx feature roadmap.

# Tasks

### T001: Phase 1 — Fix import duplicate check (Name+Manufacturer)
- **Blocked By**: []
- **Files**: PrimeRx/Pages/Admin/Medicines/Index.cshtml.cs
- **Details**: Change key from Name-only to Name+Manufacturer composite.

### T002: Phase 3 — Auto-fill GenericName/FormType/Manufacturer in purchase rows
- **Blocked By**: []
- **Files**: PrimeRx/Pages/Purchase/Create.cshtml.cs, PrimeRx/wwwroot/js/purchase.js
- **Details**: Search handler returns formType+manufacturer. JS stores & displays sub-info in each table row.

### T003: Phase 5 — Add Supplier Profit + Payable by Supplier to ReportService
- **Blocked By**: []
- **Files**: PrimeRx/Services/ReportService.cs
- **Details**: GetSupplierProfitReportAsync (cost vs MRP margin per supplier), GetPayablesBySupplierAsync.

### T004: Phase 5 — Wire new reports into Reports page
- **Blocked By**: [T003]
- **Files**: PrimeRx/Pages/Reports/Index.cshtml, PrimeRx/Pages/Reports/Index.cshtml.cs
- **Details**: Add handler methods, report cards, and display sections for both new reports.

### T005: Restart + verify
- **Blocked By**: [T001, T002, T003, T004]
