using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Expenses;

[Authorize(Policy = "StaffAccess")]
public class AddModel(ExpenseService expenseService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        public string Category { get; set; } = ExpenseCategories.Miscellaneous;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required, MaxLength(300)]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Paid By")]
        public string? PaidBy { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var expense = new Expense
        {
            ExpenseDate = Input.ExpenseDate,
            Category = Input.Category,
            Amount = Input.Amount,
            Reason = Input.Reason,
            Notes = string.IsNullOrWhiteSpace(Input.PaidBy)
                ? Input.Notes
                : $"Paid by: {Input.PaidBy}" + (string.IsNullOrWhiteSpace(Input.Notes) ? "" : $" | {Input.Notes}"),
            CreatedBy = User.Identity?.Name,
            StaffId = User.Identity?.Name
        };

        await expenseService.AddExpenseAsync(expense);

        SuccessMessage = $"Expense of {Input.Amount.ToRs()} recorded successfully.";
        Input = new InputModel();
        ModelState.Clear();

        return Page();
    }
}
