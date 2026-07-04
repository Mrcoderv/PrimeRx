using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Reports;

public class IndexModel(ReportService reportService, DashboardService dashboardService) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? ReportTitle { get; set; }
    public string? SummaryText { get; set; }
    public string? ActiveReport { get; set; }
    public SalesReportData? SalesReport { get; set; }
    public List<MedicineSalesRow>? MedicineSales { get; set; }
    public ProfitLossReport? ProfitLoss { get; set; }
    public DueCollectionReport? DueReport { get; set; }
    public List<Medicine>? InventoryMedicines { get; set; }
    public List<Medicine>? ExpiryMedicines { get; set; }
    public PurchaseReportData? PurchaseReport { get; set; }

    public List<SupplierProfitRow>? SupplierProfit { get; set; }
    public List<MonthlySupplierPurchaseRow>? MonthlySupplierPurchase { get; set; }
    public SupplierPayableReport? SupplierPayables { get; set; }
    public List<AuditLog>? AuditLogs { get; set; }

    public string SalesTrendJson { get; private set; } = "[]";
    public string MedicineChartJson { get; private set; } = "[]";
    public ReportPageStats Stats { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnGetDailySalesAsync(DateTime? date, string format = "view")
    {
        await LoadPageDataAsync();
        ActiveReport = "daily";
        var reportDate = date ?? DateTime.Today;
        var report = await reportService.GetDailySalesAsync(reportDate);
        return HandleSalesReport(report, format, $"daily-sales-{reportDate:yyyyMMdd}");
    }

    public async Task<IActionResult> OnGetMonthlySalesAsync(int? year, int? month, string format = "view")
    {
        await LoadPageDataAsync();
        ActiveReport = "monthly";
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        var report = await reportService.GetMonthlySalesAsync(y, m);
        return HandleSalesReport(report, format, $"monthly-sales-{y}-{m:D2}");
    }

    public async Task<IActionResult> OnGetMedicineSalesAsync(string format = "view")
    {
        await LoadPageDataAsync();
        ActiveReport = "medicine";
        var rows = await reportService.GetMedicineWiseSalesAsync(DateTime.Today.AddDays(-30), DateTime.Today);
        if (format == "excel")
            return File(reportService.ExportMedicineSalesToExcel(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "medicine-sales.xlsx");

        MedicineSales = rows;
        ReportTitle = "Medicine-wise Sales (Last 30 Days)";
        SummaryText = $"Total: Rs. {rows.Sum(r => r.TotalAmount):N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetProfitLossAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "pnl";
        ProfitLoss = await reportService.GetProfitLossAsync(DateTime.Today.AddMonths(-1), DateTime.Today);
        ReportTitle = "Profit & Loss (Last 30 Days)";
        SummaryText = ProfitLoss.Profit >= 0 ? $"Net Profit: Rs. {ProfitLoss.Profit:N2}" : $"Net Loss: Rs. {Math.Abs(ProfitLoss.Profit):N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetDueCollectionAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "due";
        DueReport = await reportService.GetDueCollectionAsync();
        ReportTitle = "Due Collection Report";
        SummaryText = $"Outstanding: Rs. {DueReport.OutstandingDue:N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetInventoryAsync(string format = "view")
    {
        await LoadPageDataAsync();
        ActiveReport = "inventory";
        var medicines = await reportService.GetInventoryReportAsync();
        if (format == "excel")
            return File(reportService.ExportInventoryToExcel(medicines), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory.xlsx");

        InventoryMedicines = medicines;
        ReportTitle = "Inventory Report";
        SummaryText = $"{medicines.Count} active medicines";
        return Page();
    }

    public async Task<IActionResult> OnGetExpiryAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "expiry";
        ExpiryMedicines = await reportService.GetExpiryReportAsync();
        ReportTitle = "Expiry Report (Next 90 Days)";
        SummaryText = $"{ExpiryMedicines.Count} medicines expiring soon";
        return Page();
    }

    public async Task<IActionResult> OnGetPurchaseReportAsync(DateTime? from, DateTime? to, string? supplier, string format = "view")
    {
        await LoadPageDataAsync();
        ActiveReport = "purchase";

        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate = to ?? DateTime.Today;

        var report = await reportService.GetPurchaseReportAsync(fromDate, toDate, supplier);

        if (format == "excel")
            return File(reportService.ExportPurchaseReportToExcel(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"purchase-report-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx");

        PurchaseReport = report;
        ReportTitle = report.Title;
        SummaryText = $"Rs. {report.TotalAmount:N2} · {report.PurchaseCount} purchase(s)";
        return Page();
    }

    public async Task<IActionResult> OnGetSupplierProfitAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "supplierprofit";
        SupplierProfit = await reportService.GetSupplierProfitReportAsync();
        ReportTitle = "Supplier-wise Profit Analysis";
        SummaryText = $"{SupplierProfit.Count} suppliers · Total Cost: Rs. {SupplierProfit.Sum(r => r.TotalCost):N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetMonthlySupplierAsync(int? year)
    {
        await LoadPageDataAsync();
        ActiveReport = "monthlysupplier";
        var y = year ?? DateTime.Today.Year;
        MonthlySupplierPurchase = await reportService.GetMonthlyPurchaseBySupplierAsync(y);
        ReportTitle = $"Monthly Purchase by Supplier — {y}";
        SummaryText = $"Total: Rs. {MonthlySupplierPurchase.Sum(r => r.TotalAmount):N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetSupplierPayablesAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "supplierpayables";
        SupplierPayables = await reportService.GetSupplierPayableReportAsync();
        ReportTitle = "Payables to Suppliers";
        SummaryText = $"Pending: Rs. {SupplierPayables.TotalPending:N2}";
        return Page();
    }

    public async Task<IActionResult> OnGetAuditReportAsync()
    {
        await LoadPageDataAsync();
        ActiveReport = "audit";
        AuditLogs = await reportService.GetAuditReportAsync(200);
        ReportTitle = "Audit Log (Last 200 Entries)";
        SummaryText = $"{AuditLogs.Count} entries";
        return Page();
    }

    private IActionResult HandleSalesReport(SalesReportData report, string format, string fileName)
    {
        if (format == "pdf")
            return File(reportService.ExportSalesToPdf(report), "application/pdf", $"{fileName}.pdf");

        if (format == "excel")
            return File(reportService.ExportSalesToExcel(report), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx");

        SalesReport = report;
        ReportTitle = report.Title;
        SummaryText = $"Total: Rs. {report.TotalSales:N2} · {report.BillCount} bills";
        return Page();
    }

    private async Task LoadPageDataAsync()
    {
        var summary = await dashboardService.GetSummaryAsync();
        var salesTrend = summary.SalesTrend;
        var topMeds = summary.TopMedicines;

        Stats = new ReportPageStats
        {
            TodaySales = summary.TodaySales,
            MonthSales = summary.MonthSales,
            OutstandingDue = summary.OutstandingDue,
            TodayBills = summary.TodayBills
        };

        SalesTrendJson = JsonSerializer.Serialize(salesTrend, JsonOptions);
        MedicineChartJson = JsonSerializer.Serialize(topMeds, JsonOptions);
    }
}

public class ReportPageStats
{
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public decimal OutstandingDue { get; set; }
    public int TodayBills { get; set; }
}
