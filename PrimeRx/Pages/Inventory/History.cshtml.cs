using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class HistoryModel(InventoryService inventoryService) : PageModel
{
    public List<InventoryTransaction> Transactions { get; set; } = [];

    public async Task OnGetAsync()
    {
        Transactions = await inventoryService.GetTransactionHistoryAsync();
    }
}
