using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Inventory;

public class HistoryModel(InventoryService inventoryService) : PageModel
{
    public List<InventoryTransaction> Transactions { get; set; } = [];

    public async Task OnGetAsync()
    {
        Transactions = await inventoryService.GetTransactionHistoryAsync();
    }
}
