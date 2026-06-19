using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class AdjustModel(InventoryService inventoryService) : PageModel
{
    [BindProperty]
    public StockAdjustmentRequest Input { get; set; } = new();

    public List<SelectListItem> MedicineOptions { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadMedicinesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await inventoryService.AdjustStockAsync(Input);
            Message = "Stock adjusted successfully.";
            Input = new StockAdjustmentRequest();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadMedicinesAsync();
        return Page();
    }

    private async Task LoadMedicinesAsync()
    {
        var medicines = await inventoryService.GetAllAsync();
        MedicineOptions = medicines.Select(m => new SelectListItem($"{m.Name} (Stock: {m.StockQuantity})", m.Id.ToString())).ToList();
    }
}
