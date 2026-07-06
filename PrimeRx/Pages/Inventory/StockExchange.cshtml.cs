using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class StockExchangeModel(InventoryService inventoryService) : PageModel
{
    [BindProperty]
    public ExchangeInput Input { get; set; } = new();

    public List<Medicine> Medicines { get; set; } = [];
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public List<InventoryTransaction> RecentExchanges { get; set; } = [];

    public async Task OnGetAsync(string? message, bool error)
    {
        Message = message;
        IsError = error;
        Medicines = await inventoryService.GetAllAsync(includeInactive: false);
        var allTx = await inventoryService.GetTransactionHistoryAsync();
        RecentExchanges = allTx.Where(t => t.TransactionType == TransactionTypes.Exchange).Take(20).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Quantity <= 0)
        {
            IsError = true;
            Message = "Quantity must be greater than zero.";
            return await ReloadPageAsync();
        }

        if (string.IsNullOrWhiteSpace(Input.OtherPharmacy))
        {
            IsError = true;
            Message = "Other pharmacy name is required.";
            return await ReloadPageAsync();
        }

        try
        {
            await inventoryService.ExchangeStockAsync(
                Input.MedicineId,
                Input.Quantity,
                Input.OtherPharmacy.Trim(),
                Input.Reference?.Trim());

            return RedirectToPage(new { message = $"Exchanged {Input.Quantity} units to {Input.OtherPharmacy.Trim()}", error = false });
        }
        catch (Exception ex)
        {
            IsError = true;
            Message = ex.Message;
            return await ReloadPageAsync();
        }
    }

    private async Task<IActionResult> ReloadPageAsync()
    {
        Medicines = await inventoryService.GetAllAsync(includeInactive: false);
        var allTx = await inventoryService.GetTransactionHistoryAsync();
        RecentExchanges = allTx.Where(t => t.TransactionType == TransactionTypes.Exchange).Take(20).ToList();
        return Page();
    }

    public class ExchangeInput
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string OtherPharmacy { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }
}
