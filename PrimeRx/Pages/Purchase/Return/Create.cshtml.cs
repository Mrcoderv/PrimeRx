using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase.Return;

public class CreateModel(
    PurchaseReturnService returnService,
    InventoryService inventoryService,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public PurchaseReturnFormInput Input { get; set; } = new();

    [BindProperty]
    public string ItemsJson { get; set; } = "[]";

    public string[] Reasons => PurchaseReturnReasons.All;
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        Input.ReturnDate = DateTime.Today;
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
            batchNumber = m.BatchNumber
        }));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<PurchaseReturnLineItem>>(ItemsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            if (!items.Any())
            {
                ErrorMessage = "Please add at least one medicine to return.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.SupplierName))
            {
                ErrorMessage = "Supplier name is required.";
                return Page();
            }

            var user = await userManager.GetUserAsync(User);
            var request = new PurchaseReturnCreateRequest
            {
                ReturnDate = Input.ReturnDate,
                SupplierName = Input.SupplierName,
                InvoiceNumber = Input.InvoiceNumber,
                Reason = Input.Reason,
                Notes = Input.Notes,
                Items = items
            };

            var result = await returnService.CreateAsync(request, user?.UserName);

            return RedirectToPage("/Purchase/Return/Index",
                new { msg = $"Return recorded — Rs. {result.TotalAmount:N2} credit note issued for {result.SupplierName}." });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

public class PurchaseReturnFormInput
{
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public string SupplierName { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string Reason { get; set; } = PurchaseReturnReasons.Other;
    public string? Notes { get; set; }
}
