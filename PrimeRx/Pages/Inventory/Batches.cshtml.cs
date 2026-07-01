using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class BatchesModel(InventoryService inventoryService) : PageModel
{
    public List<InventoryBatch> Batches { get; set; } = [];
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(string? search)
    {
        SearchTerm = search;
        var all = await inventoryService.GetBatchesAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            all = all.Where(b =>
                b.Medicine.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.BatchNumber.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Batches = all.Where(b => b.Quantity > 0).ToList();
    }
}
