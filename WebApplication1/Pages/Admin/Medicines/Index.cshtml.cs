using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Admin.Medicines;

public class IndexModel(InventoryService inventoryService) : PageModel
{
    public List<Medicine> Medicines { get; set; } = [];
    public string? Search { get; set; }
    public string? Message { get; set; }

    public async Task OnGetAsync(string? search, string? message)
    {
        Search = search;
        Message = message;
        Medicines = await inventoryService.GetAllAsync(search, includeInactive: true);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await inventoryService.DeleteAsync(id);
        return RedirectToPage(new { message = "Medicine deactivated." });
    }
}
