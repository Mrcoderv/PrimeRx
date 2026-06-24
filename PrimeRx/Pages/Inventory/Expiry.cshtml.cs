using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class ExpiryModel : PageModel
{
    private readonly InventoryService _inventoryService;

    public ExpiryModel(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public List<Medicine> ExpiringMedicines { get; set; } = new();
    public string? Message { get; set; }
    public int SelectedDays { get; set; } = 90;

    public async Task OnGetAsync(int? days = 90)
    {
        SelectedDays = days ?? 90;
        ExpiringMedicines = await _inventoryService.GetExpiringMedicinesAsync(SelectedDays);
        
        if (!ExpiringMedicines.Any())
            Message = $"No medicines expiring in the next {SelectedDays} days.";
    }
}
