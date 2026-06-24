using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly DueService _dueService;
    private readonly InventoryService _inventoryService;

    public IndexModel(DueService dueService, InventoryService inventoryService)
    {
        _dueService = dueService;
        _inventoryService = inventoryService;
    }

    public List<Bill> CustomerDues { get; set; } = new();
    public List<Medicine> Expiring { get; set; } = new();
    public List<Medicine> LowStock { get; set; } = new();
    
    public int TotalDueAmount { get; set; }
    public int TotalExpiringCount { get; set; }
    public int TotalLowStockCount { get; set; }

    public async Task OnGetAsync()
    {
        CustomerDues = await _dueService.GetDueBillsAsync();
        Expiring = await _inventoryService.GetExpiringMedicinesAsync(30); // tighter window for notifications
        LowStock = await _inventoryService.GetLowStockAsync();
        
        TotalDueAmount = CustomerDues.Count;
        TotalExpiringCount = Expiring.Count;
        TotalLowStockCount = LowStock.Count;
    }
}
