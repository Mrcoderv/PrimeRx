using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase;

public class IndexModel(PurchaseService purchaseService) : PageModel
{
    public List<PrimeRx.Models.Purchase> Purchases { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SupplierFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public async Task OnGetAsync(string? supplier, DateTime? from, DateTime? to, string? msg)
    {
        SupplierFilter = supplier;
        FromDate = from;
        ToDate = to;
        Message = msg;

        var all = await purchaseService.GetAllAsync(500);

        if (!string.IsNullOrWhiteSpace(supplier))
            all = all.Where(p => p.SupplierName.Contains(supplier, StringComparison.OrdinalIgnoreCase)).ToList();

        if (from.HasValue)
            all = all.Where(p => p.PurchaseDate.Date >= from.Value.Date).ToList();

        if (to.HasValue)
            all = all.Where(p => p.PurchaseDate.Date <= to.Value.Date).ToList();

        Purchases = all;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await purchaseService.DeleteAsync(id);
            return RedirectToPage(new { msg = "Purchase deleted and stock reversed." });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync(null, null, null, null);
            return Page();
        }
    }
}
