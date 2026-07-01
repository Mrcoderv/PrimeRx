using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase.Return;

public class IndexModel(PurchaseReturnService returnService) : PageModel
{
    public List<PurchaseReturn> Returns { get; set; } = [];
    public List<CreditNote> CreditNotes { get; set; } = [];
    public string? Message { get; set; }

    public async Task OnGetAsync(string? msg)
    {
        Message = msg;
        Returns = await returnService.GetAllAsync();
        CreditNotes = await returnService.GetCreditNotesAsync();
    }
}
