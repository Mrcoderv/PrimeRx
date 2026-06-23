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
            text = string.IsNullOrWhiteSpace(m.GenericName)
                ? $"{m.Name} — Rs.{m.MRP:N2} · Stock: {m.StockQuantity}"
                : $"{m.Name} ({m.GenericName}) — Rs.{m.MRP:N2} · Stock: {m.StockQuantity}",
            name = m.Name,
            genericName = m.GenericName,
            mrp = m.MRP,
            stockQuantity = m.StockQuantity,
            discountPercent = m.DiscountPercent
        }));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var items = JsonSerializer.Deserialize<List<BillLineItem>>(Input.ItemsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var request = new CreateBillRequest
            {
                CustomerName = Input.CustomerName,
                CustomerPhone = Input.CustomerPhone,
                PaymentMethod = Input.PaymentMethod,
                DiscountAmount = Input.DiscountAmount,
                Items = items
            };

            var user = await userManager.GetUserAsync(User);
            var bill = await billingService.CreateBillAsync(request, user?.Id, user?.UserName);

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

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string CustomerPhone { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "Cash";
        public decimal DiscountAmount { get; set; }
        public string ItemsJson { get; set; } = "[]";
    }
}
