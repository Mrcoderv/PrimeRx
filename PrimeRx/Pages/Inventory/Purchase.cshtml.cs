using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class PurchaseModel(InventoryService inventoryService) : PageModel
{
    [BindProperty]
    public PurchaseEntryRequest Input { get; set; } = new();

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
            await inventoryService.RecordPurchaseAsync(Input);
            Message = "Stock added successfully.";
            Input = new PurchaseEntryRequest();
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
        MedicineOptions = medicines.Select(m => new SelectListItem(m.Name, m.Id.ToString())).ToList();
    }
}
