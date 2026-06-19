using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Reports;

public class IndexModel(ReportService reportService, DashboardService dashboardService) : PageModel
{
    public string? ReportTitle { get; set; }
    public string? SummaryText { get; set; }
    public SalesReportData? SalesReport { get; set; }
    public List<MedicineSalesRow>? MedicineSales { get; set; }
    public ProfitLossReport? ProfitLoss { get; set; }
    public DueCollectionReport? DueReport { get; set; }
    public List<Medicine>? InventoryMedicines { get; set; }
    public List<Medicine>? ExpiryMedicines { get; set; }
    public List<DailySalesPoint> SalesTrend { get; set; } = [];
    public List<MedicineSalesRow> MedicineSalesChart { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadChartDataAsync();
    }

    public async Task<IActionResult> OnGetDailySalesAsync(DateTime? date, string format = "view")
    {
        await LoadChartDataAsync();
        var reportDate = date ?? DateTime.Today;
        var report = await reportService.GetDailySalesAsync(reportDate);
        return HandleSalesReport(report, format, $"daily-sales-{reportDate:yyyyMMdd}");
    }

    public async Task<IActionResult> OnGetMonthlySalesAsync(int? year, int? month, string format = "view")
    {
        await LoadChartDataAsync();
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        var report = await reportService.GetMonthlySalesAsync(y, m);
        return HandleSalesReport(report, format, $"monthly-sales-{y}-{m:D2}");
    }

    public async Task<IActionResult> OnGetMedicineSalesAsync(string format = "view")
    {
        await LoadChartDataAsync();
        var rows = await reportService.GetMedicineWiseSalesAsync();
        if (format == "excel")
            return File(reportService.ExportMedicineSalesToExcel(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "medicine-sales.xlsx");

        MedicineSales = rows;
        ReportTitle = "Medicine-wise Sales";
        return Page();
    }

    public async Task<IActionResult> OnGetProfitLossAsync()
    {
        await LoadChartDataAsync();
        ProfitLoss = await reportService.GetProfitLossAsync(DateTime.Today.AddMonths(-1), DateTime.Today);
        ReportTitle = "Profit & Loss (Last 30 Days)";
        return Page();
    }

    public async Task<IActionResult> OnGetDueCollectionAsync()
    {
        await LoadChartDataAsync();
        DueReport = await reportService.GetDueCollectionAsync();
        ReportTitle = "Due Collection Report";
        return Page();
    }

    public async Task<IActionResult> OnGetInventoryAsync(string format = "view")
    {
        await LoadChartDataAsync();
        var medicines = await reportService.GetInventoryReportAsync();
        if (format == "excel")
            return File(reportService.ExportInventoryToExcel(medicines), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory.xlsx");

        InventoryMedicines = medicines;
        ReportTitle = "Inventory Report";
        return Page();
    }

    public async Task<IActionResult> OnGetExpiryAsync()
    {
        await LoadChartDataAsync();
        ExpiryMedicines = await reportService.GetExpiryReportAsync();
        ReportTitle = "Expiry Report (90 days)";
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
        SummaryText = $"Total: {report.TotalSales:N2} ({report.BillCount} bills)";
        return Page();
    }

    private async Task LoadChartDataAsync()
    {
        SalesTrend = await dashboardService.GetSalesTrendAsync(30);
        MedicineSalesChart = await reportService.GetMedicineWiseSalesAsync(DateTime.Today.AddDays(-30), DateTime.Today);
    }
}
