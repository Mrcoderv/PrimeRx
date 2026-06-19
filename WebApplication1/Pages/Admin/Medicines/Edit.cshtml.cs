using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Admin.Medicines;

public class EditModel(InventoryService inventoryService) : PageModel
{
    [BindProperty]
    public Medicine Medicine { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var medicine = await inventoryService.GetByIdAsync(id);
        if (medicine is null) return NotFound();

        Medicine = medicine;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        await inventoryService.UpdateAsync(Medicine);
        return RedirectToPage("/Admin/Medicines/Index", new { message = "Medicine updated." });
    }
}
