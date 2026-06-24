using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Expenses;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(ExpenseService expenseService) : PageModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public string? Message { get; set; }

    public List<Expense> Expenses { get; set; } = [];
    public Dictionary<string, decimal> ByCategory { get; set; } = [];
    public decimal Total { get; set; }

    public DateTime PreviousMonth { get; set; }
    public DateTime NextMonth { get; set; }

    // Edit binding
    [BindProperty]
    public EditInput Edit { get; set; } = new();

    public class EditInput
    {
        public int Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = ExpenseCategories.Miscellaneous;
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string? StaffId { get; set; }
        public string? Notes { get; set; }
    }

    public async Task OnGetAsync(int? year, int? month, string? message)
    {
        Message = message;
        await LoadAsync(year, month);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int? year, int? month)
    {
        await expenseService.DeleteAsync(id);
        await LoadAsync(year, month);
        return RedirectToPage(new { year = Year, month = Month, message = "Expense deleted." });
    }

    public async Task<IActionResult> OnPostEditAsync(int? year, int? month)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(year, month);
            return Page();
        }

        var existing = await expenseService.GetByIdAsync(Edit.Id);
        if (existing == null) return NotFound();

        existing.ExpenseDate = Edit.ExpenseDate;
        existing.Category = Edit.Category;
        existing.Amount = Edit.Amount;
        existing.Reason = Edit.Reason;
        existing.StaffId = Edit.StaffId;
        existing.Notes = Edit.Notes;
        existing.LastModifiedBy = User.Identity?.Name;

        await expenseService.UpdateAsync(existing);
        return RedirectToPage(new { year = Edit.ExpenseDate.Year, month = Edit.ExpenseDate.Month, message = "Expense updated." });
    }

    private async Task LoadAsync(int? year, int? month)
    {
        var today = DateTime.Today;
        Year = year ?? today.Year;
        Month = month is >= 1 and <= 12 ? month.Value : today.Month;

        var current = new DateTime(Year, Month, 1);
        MonthLabel = current.ToString("MMMM yyyy");
        PreviousMonth = current.AddMonths(-1);
        NextMonth = current.AddMonths(1);

        Expenses = await expenseService.GetMonthlyExpensesAsync(Year, Month);
        ByCategory = await expenseService.GetExpensesByCategoryAsync(Year, Month);
        Total = Expenses.Sum(e => e.Amount);
    }
}
