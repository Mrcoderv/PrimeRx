using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Billing;

public class EditModel(
    BillingService billingService,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public BillingFormInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string InitialItemsJson { get; set; } = "[]";
    public Bill? Bill { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var bill = await billingService.GetByIdAsync(id);
        if (bill is null)
            return NotFound();

        if (bill.Status == BillStatuses.Cancelled)
        {
            ErrorMessage = "This bill is cancelled and cannot be edited.";
            Bill = bill;
            return Page();
        }

        Bill = bill;
        Input.CustomerName = bill.CustomerName;
        Input.CustomerPhone = bill.CustomerPhone;
        Input.PaymentMethod = bill.PaymentMethod;

        var items = bill.SaleItems.Select(i => new
        {
            medicineId = i.MedicineId,
            medicineName = i.MedicineName,
            rate = i.Rate,
            quantity = i.Quantity,
            availableStock = i.Batch?.Quantity ?? 999,
            discountPercent = i.DiscountPercent,
            discountAmount = i.DiscountAmount,
            selectedBatchId = i.BatchId,
            batchNumber = i.BatchNumber
        });

        InitialItemsJson = JsonSerializer.Serialize(items);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var bill = await billingService.GetByIdAsync(id);
        if (bill is null)
            return NotFound();

        if (bill.Status == BillStatuses.Cancelled)
        {
            ErrorMessage = "This bill is cancelled and cannot be edited.";
            Bill = bill;
            return Page();
        }

        if (Input.PaymentMethod == "Due" && string.IsNullOrWhiteSpace(Input.CustomerPhone))
        {
            ModelState.AddModelError("Input.CustomerPhone", "Phone number is required when payment is Due");
        }

        if (!ModelState.IsValid)
        {
            Bill = bill;
            return Page();
        }

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

            var staffName = !string.IsNullOrEmpty(user?.UserName) ? user.UserName :
                           (!string.IsNullOrEmpty(user?.Email) ? user.Email : "Admin");

            await billingService.UpdateBillAsync(id, request, user?.Id, staffName);

            return RedirectToPage("/Billing/History", new { saved = true });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Bill = bill;
            return Page();
        }
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
