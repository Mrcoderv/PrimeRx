using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Billing;

public class IndexModel(
    BillingService billingService,
    InventoryService inventoryService,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public BillingFormInput Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int? LastBillId { get; set; }

    public void OnGet(string? success, int? billId)
    {
        if (!string.IsNullOrEmpty(success))
        {
            SuccessMessage = success;
            LastBillId = billId;
        }
    }

    public async Task<IActionResult> OnGetSearchAsync(string term)
    {
        var results = await inventoryService.SearchMedicinesAsync(term);
        return new JsonResult(results.Select(m => new
        {
            id = m.Id,
            name = m.Name,
            genericName = m.GenericName,
            mrp = m.MRP,
            stockQuantity = m.StockQuantity,
            discountPercent = m.DiscountPercent
        }));
    }

    public async Task<IActionResult> OnGetBatchesAsync(int medicineId)
    {
        var batches = await inventoryService.GetBatchesAsync(medicineId);
        return new JsonResult(batches
            .Where(b => b.Quantity > 0)
            .Select(b => new
            {
                id = b.Id,
                batchNumber = b.BatchNumber,
                quantity = b.Quantity,
                expiryDate = b.ExpiryDate.HasValue
                    ? b.ExpiryDate.Value.ToString("MMM yyyy")
                    : (string?)null
            }));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.PaymentMethod == "Due" && string.IsNullOrWhiteSpace(Input.CustomerPhone))
        {
            ModelState.AddModelError("Input.CustomerPhone", "Phone number is required when payment is Due");
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var items = JsonSerializer.Deserialize<List<BillLineItem>>(Input.ItemsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var request = new CreateBillRequest
            {
                CustomerName = Input.CustomerName,
                CustomerPhone = Input.CustomerPhone ?? string.Empty,
                PaymentMethod = Input.PaymentMethod,
                DiscountAmount = 0m,
                Items = items
            };

            var user = await userManager.GetUserAsync(User);
            
            // Prioritize Username, fallback to Email
            var staffName = !string.IsNullOrEmpty(user?.UserName) ? user.UserName : 
                           (!string.IsNullOrEmpty(user?.Email) ? user.Email : "Admin");

            var bill = await billingService.CreateBillAsync(request, user?.Id, staffName);

            return RedirectToPage(new { success = $"Bill {bill.BillNumber} created successfully!", billId = bill.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnGetDownloadPdfAsync(int billId)
    {
        var pdf = await billingService.GenerateInvoicePdfAsync(billId);
        return File(pdf, "application/pdf", $"Invoice-{billId}.pdf");
    }

    public class BillingFormInput
    {
        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; } = string.Empty;

        [Phone]
        public string? CustomerPhone { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public string ItemsJson { get; set; } = "[]";
    }
}