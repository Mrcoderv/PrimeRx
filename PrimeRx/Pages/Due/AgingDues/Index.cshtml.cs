using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Due.AgingDues;

public class IndexModel(AgingDueService agingDueService) : PageModel
{
    public AgingDueReport Report { get; set; } = new();
    public string? PartyFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? AsOnDate { get; set; }

    public async Task OnGetAsync(
        string? party,
        DateTime? from,
        DateTime? to,
        DateTime? asOn)
    {
        PartyFilter = party;
        FromDate = from;
        ToDate = to;
        AsOnDate = asOn;
        Report = await agingDueService.GetReportAsync(party, from, to, asOn);
    }

    public async Task<IActionResult> OnGetSupplierPdfAsync(
        DateTime? from,
        DateTime? to,
        DateTime? asOn)
    {
        var report = await agingDueService.GetReportAsync("Supplier", from, to, asOn);
        var pdf = agingDueService.ExportToPdf(report);
        return File(pdf, "application/pdf", "supplier-dues.pdf");
    }

    public async Task<IActionResult> OnGetCustomerPdfAsync(
        DateTime? from,
        DateTime? to,
        DateTime? asOn)
    {
        var report = await agingDueService.GetReportAsync("Customer", from, to, asOn);
        var pdf = agingDueService.ExportToPdf(report);
        return File(pdf, "application/pdf", "customer-dues.pdf");
    }
}
