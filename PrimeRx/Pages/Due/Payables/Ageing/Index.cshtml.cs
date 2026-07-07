using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Due.Payables.Ageing;

public class IndexModel(PayableService payableService) : PageModel
{
    public PayableAgingReport Report { get; set; } = new();
    public string? SupplierFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public async Task OnGetAsync(string? supplier, DateTime? from, DateTime? to)
    {
        SupplierFilter = supplier;
        FromDate = from;
        ToDate = to;
        Report = await payableService.GetPayableAgingReportAsync(supplier, from, to);
    }

    public async Task<IActionResult> OnGetPdfAsync(string? supplier, DateTime? from, DateTime? to)
    {
        var report = await payableService.GetPayableAgingReportAsync(supplier, from, to);
        var pdf = payableService.ExportToPdf(report);
        return File(pdf, "application/pdf", "ageing-dues.pdf");
    }

    public async Task<IActionResult> OnGetExcelAsync(string? supplier, DateTime? from, DateTime? to)
    {
        var report = await payableService.GetPayableAgingReportAsync(supplier, from, to);
        var excel = payableService.ExportToExcel(report);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ageing-dues.xlsx");
    }
}
