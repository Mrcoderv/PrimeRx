using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

public class ExpenseService(ApplicationDbContext context)
{
    public async Task AddExpenseAsync(Expense expense)
    {
        expense.CreatedAt = DateTime.Now;
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
    }

    public async Task<Expense?> GetByIdAsync(int id)
        => await context.Expenses.FindAsync(id);

    public async Task UpdateAsync(Expense expense)
    {
        expense.LastModifiedAt = DateTime.Now;
        context.Expenses.Update(expense);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var expense = await context.Expenses.FindAsync(id);
        if (expense != null)
        {
            context.Expenses.Remove(expense);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Expense>> GetMonthlyExpensesAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await context.Expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate < end)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalExpensesAsync(DateTime start, DateTime end)
    {
        return await context.Expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate < end)
            .SumAsync(e => e.Amount);
    }

    public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await context.Expenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate < end)
            .GroupBy(e => e.Category)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(e => e.Amount));
    }
}
