using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Reports;

public class VendorReportModel(ReportService reportService) : PageModel
{
    public VendorReportData? Report { get; set; }
    public string? ActiveFilter { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? vendor, DateTime? from, DateTime? to)
    {
        try
        {
            Report = await reportService.GetVendorReportAsync(vendor, from, to);
            ActiveFilter = vendor;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating report: {ex.Message}";
            Report = new VendorReportData();
        }
    }

    public async Task<IActionResult> OnGetExportPdfAsync(string? vendor, DateTime? from, DateTime? to)
    {
        Report = await reportService.GetVendorReportAsync(vendor, from, to);
        var pdfBytes = reportService.ExportVendorReportToPdf(Report);
        var fileName = !string.IsNullOrEmpty(vendor)
            ? $"vendor-report-{vendor.Replace(" ", "-")}.pdf"
            : "vendor-report-all.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    public async Task<IActionResult> OnGetExportExcelAsync(string? vendor, DateTime? from, DateTime? to)
    {
        Report = await reportService.GetVendorReportAsync(vendor, from, to);
        var excelBytes = reportService.ExportVendorReportToExcel(Report);
        var fileName = !string.IsNullOrEmpty(vendor)
            ? $"vendor-report-{vendor.Replace(" ", "-")}.xlsx"
            : "vendor-report-all.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
