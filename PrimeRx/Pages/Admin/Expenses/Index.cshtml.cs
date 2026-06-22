using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Expenses;

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

    public async Task OnGetAsync(int? year, int? month, string? message)
    {
        Message = message;

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
