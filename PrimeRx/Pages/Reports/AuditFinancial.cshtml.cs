using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Reports;

public class AuditFinancialModel(AuditFinancialService auditService, ReportService reportService) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditFinancialData? Report { get; set; }
    public List<FiscalYear> FiscalYears { get; set; } = [];
    public int SelectedStartYear { get; set; }
    public string? ErrorMessage { get; set; }

    public string BalanceSheetJson { get; private set; } = "[]";
    public string IncomeJson { get; private set; } = "[]";

    public async Task OnGetAsync(int? startYear)
    {
        FiscalYears = auditService.GetAvailableFiscalYears();
        SelectedStartYear = startYear ?? FiscalYears.First().StartYear;
        var fy = FiscalYears.FirstOrDefault(y => y.StartYear == SelectedStartYear) ?? FiscalYears.First();

        try
        {
            Report = await auditService.GetAuditFinancialDataAsync(fy);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating report: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnGetExportPdfAsync(int startYear)
    {
        FiscalYears = auditService.GetAvailableFiscalYears();
        var fy = FiscalYears.FirstOrDefault(y => y.StartYear == startYear) ?? FiscalYears.First();
        Report = await auditService.GetAuditFinancialDataAsync(fy);
        var pdfBytes = reportService.ExportAuditFinancialToPdf(Report);
        return File(pdfBytes, "application/pdf", $"audit-report-{fy.Label}.pdf");
    }

    public async Task<IActionResult> OnGetExportExcelAsync(int startYear)
    {
        FiscalYears = auditService.GetAvailableFiscalYears();
        var fy = FiscalYears.FirstOrDefault(y => y.StartYear == startYear) ?? FiscalYears.First();
        Report = await auditService.GetAuditFinancialDataAsync(fy);
        var excelBytes = reportService.ExportAuditFinancialToExcel(Report);
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"audit-report-{fy.Label}.xlsx");
    }

    public async Task<IActionResult> OnGetExportStatementAsync(int startYear, string statement)
    {
        FiscalYears = auditService.GetAvailableFiscalYears();
        var fy = FiscalYears.FirstOrDefault(y => y.StartYear == startYear) ?? FiscalYears.First();
        Report = await auditService.GetAuditFinancialDataAsync(fy);

        var excelBytes = statement.ToLower() switch
        {
            "balancesheet" => reportService.ExportBalanceSheetToExcel(Report),
            "incomestatement" => reportService.ExportIncomeStatementToExcel(Report),
            "cashflow" => reportService.ExportCashFlowToExcel(Report),
            _ => reportService.ExportAuditFinancialToExcel(Report)
        };

        var fileName = statement.ToLower() switch
        {
            "balancesheet" => $"balance-sheet-{fy.Label}.xlsx",
            "incomestatement" => $"income-statement-{fy.Label}.xlsx",
            "cashflow" => $"cash-flow-{fy.Label}.xlsx",
            _ => $"audit-report-{fy.Label}.xlsx"
        };

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
