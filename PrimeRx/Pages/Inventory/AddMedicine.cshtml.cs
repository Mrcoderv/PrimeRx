using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Inventory;

public class AddMedicineModel(InventoryService inventoryService) : PageModel
{
    [BindProperty]
    public Medicine Medicine { get; set; } = new() { LowStockThreshold = 10, IsActive = true };

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        await inventoryService.CreateAsync(Medicine);
        return RedirectToPage("/Inventory/Index", new { message = "Medicine added successfully." });
    }
}
