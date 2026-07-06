using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase;

public class EditModel(
    PurchaseService purchaseService,
    InventoryService inventoryService,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public PurchaseCreateRequest Input { get; set; } = new();

    [BindProperty]
    public string ItemsJson { get; set; } = "[]";

    public int PurchaseId { get; set; }
    public decimal MarginPercent { get; set; } = 16m;
    public List<string> KnownSuppliers { get; set; } = [];
    public string ExistingItemsJson { get; set; } = "[]";
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var purchase = await purchaseService.GetByIdAsync(id);
        if (purchase is null) return NotFound();

        PurchaseId = id;
        MarginPercent = await inventoryService.GetDefaultMarginPercentAsync();
        KnownSuppliers = await purchaseService.GetSuppliersAsync();

        Input = new PurchaseCreateRequest
        {
            SupplierName = purchase.SupplierName,
            SupplierPhone = purchase.SupplierPhone,
            InvoiceNumber = purchase.InvoiceNumber,
            PurchaseDate = purchase.PurchaseDate,
            Notes = purchase.Notes
        };

        var existingItems = purchase.Items.Select(i => new PurchaseLineItem
        {
            Id = i.Id,
            MedicineId = i.MedicineId,
            MedicineName = i.MedicineName,
            Quantity = i.Quantity,
            FreeQuantity = i.FreeQuantity,
            DiscountPercent = i.DiscountPercent,
            PurchasePrice = i.PurchasePrice,
            MRP = i.MRP,
            ConversionCharge = i.ConversionCharge,
            BatchNumber = i.BatchNumber,
            ExpiryDate = i.ExpiryDate
        }).ToList();

        ExistingItemsJson = JsonSerializer.Serialize(existingItems, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return Page();
    }

    public async Task<IActionResult> OnGetSearchAsync(string term)
    {
        var results = await inventoryService.GetAllAsync(term, includeInactive: false);
        return new JsonResult(results.Select(m => new
        {
            id = m.Id,
            name = m.Name,
            genericName = m.GenericName,
            manufacturer = m.Manufacturer,
            formType = m.FormType,
            stockQuantity = m.StockQuantity,
            lowStockThreshold = m.LowStockThreshold,
            purchasePrice = m.PurchasePrice,
            mrp = m.MRP,
            isMaster = false
        }));
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(id);
            return Page();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PurchaseLineItem>>(ItemsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            if (!items.Any())
            {
                ErrorMessage = "Please add at least one medicine item.";
                await OnGetAsync(id);
                return Page();
            }

            Input.Items = items;

            var user = await userManager.GetUserAsync(User);
            await purchaseService.UpdateAsync(id, Input, user?.UserName);

            return RedirectToPage("/Purchase/Index", new { msg = "Purchase updated successfully." });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync(id);
            return Page();
        }
    }
}
