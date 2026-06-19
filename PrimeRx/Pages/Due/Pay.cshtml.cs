using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Models.ViewModels;
using PrimeRx.Services;

namespace PrimeRx.Pages.Due;

public class PayModel(DueService dueService, BillingService billingService) : PageModel
{
    [BindProperty]
    public RecordDuePaymentRequest Input { get; set; } = new();

    public Bill? Bill { get; set; }
    public List<DuePayment> PaymentHistory { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Bill = await billingService.GetByIdAsync(id);
        if (Bill is null) return Page();

        Input.BillId = id;
        Input.AmountPaid = Bill.DueAmount;
        PaymentHistory = await dueService.GetPaymentHistoryAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Bill = await dueService.RecordPaymentAsync(Input);
            Message = "Payment recorded successfully.";
            PaymentHistory = await dueService.GetPaymentHistoryAsync(Input.BillId);

            if (Bill.DueAmount <= 0)
                return RedirectToPage("/Due/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Bill = await billingService.GetByIdAsync(Input.BillId);
            PaymentHistory = await dueService.GetPaymentHistoryAsync(Input.BillId);
        }

        return Page();
    }
}
