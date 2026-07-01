using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase;

public class CreateModel(
    PurchaseService purchaseService,
    InventoryService inventoryService,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public PurchaseCreateRequest Input { get; set; } = new();

    [BindProperty]
    public string ItemsJson { get; set; } = "[]";

    public decimal MarginPercent { get; set; } = 16m;
    public List<Supplier> KnownSuppliers { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        MarginPercent = await inventoryService.GetDefaultMarginPercentAsync();
        KnownSuppliers = await db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        Input.PurchaseDate = DateTime.Today;
    }

    public async Task<IActionResult> OnGetSearchAsync(string term)
    {
        var results = await inventoryService.GetAllAsync(term, includeInactive: false);
        return new JsonResult(results.Select(m => new
        {
            id = m.Id,
            name = m.Name,
            genericName = m.GenericName,
            stockQuantity = m.StockQuantity,
            purchasePrice = m.PurchasePrice,
            mrp = m.MRP
        }));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PurchaseLineItem>>(ItemsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            if (!items.Any())
            {
                ErrorMessage = "Please add at least one medicine item.";
                await OnGetAsync();
                return Page();
            }

            Input.Items = items;

            var user = await userManager.GetUserAsync(User);
            var purchase = await purchaseService.CreateAsync(Input, user?.UserName);

            var creditNote = Input.PaymentType == "Credit"
                ? " — a payable has been recorded in Payables."
                : string.Empty;

            return RedirectToPage("/Purchase/Index",
                new { msg = $"Purchase recorded — {purchase.Items.Count} item(s) from {purchase.SupplierName}{creditNote}" });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }
}
