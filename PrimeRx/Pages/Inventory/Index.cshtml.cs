using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class IndexModel(InventoryService inventoryService) : PageModel
{
    public List<Medicine> Medicines { get; set; } = [];
    public List<Medicine> LowStock { get; set; } = [];
    public List<Medicine> ExpiringSoon { get; set; } = [];
    public Dictionary<int, List<InventoryBatch>> MedicineBatches { get; set; } = [];
    public string? Search { get; set; }
    public string? Message { get; set; }

    public async Task OnGetAsync(string? search, string? message)
    {
        Search = search;
        Message = message;
        Medicines = await inventoryService.GetAllAsync(search);
        LowStock = await inventoryService.GetLowStockAsync();
        ExpiringSoon = await inventoryService.GetExpiringSoonAsync();

        var allBatches = await inventoryService.GetBatchesAsync();
        MedicineBatches = allBatches.GroupBy(b => b.MedicineId).ToDictionary(g => g.Key, g => g.ToList());
    }
}
