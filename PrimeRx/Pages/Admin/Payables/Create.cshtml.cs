using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Payables;

[Authorize(Policy = "AdminOnly")]
public class CreateModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(200)]
        [Display(Name = "Supplier / Vendor Name")]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Invoice No")]
        public string? InvoiceNo { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        [Display(Name = "Total Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Description / Notes")]
        public string? Description { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        db.Payables.Add(new Payable
        {
            SupplierName = Input.SupplierName,
            InvoiceNo = Input.InvoiceNo,
            Amount = Input.Amount,
            DueDate = Input.DueDate,
            Description = Input.Description,
            Status = PayableStatus.Pending
        });

        await db.SaveChangesAsync();
        return RedirectToPage("/Admin/Payables/Index", new { message = $"Payable for {Input.SupplierName} added successfully." });
    }
}
