using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Billing;

public class HistoryModel(ApplicationDbContext context, BillingService billingService) : PageModel
{
    public List<Bill> Bills { get; set; } = [];
    public string? Search { get; set; }
    public bool ShowSaved { get; set; }

    public async Task OnGetAsync(string? search, bool? saved)
    {
        Search = search;
        ShowSaved = saved == true;

        var query = context.Bills.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.BillNumber.ToLower().Contains(term) ||
                b.CustomerName.ToLower().Contains(term) ||
                (b.CustomerPhone ?? "").Contains(term) ||
                (b.StaffName ?? "").ToLower().Contains(term));
        }

        Bills = await query
            .OrderByDescending(b => b.BillDate)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        try
        {
            await billingService.CancelBillAsync(id, reason);
            TempData["Success"] = "Bill cancelled successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
