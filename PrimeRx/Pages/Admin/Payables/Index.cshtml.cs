using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Payables;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(ApplicationDbContext db) : PageModel
{
    public List<Payable> Payables { get; set; } = [];
    public string Filter { get; set; } = "All";
    public string? SupplierSearch { get; set; }
    public string? Message { get; set; }
    public decimal TotalPending { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal PaidThisMonth { get; set; }
    public int OverdueCount { get; set; }
    public int DueSoonCount { get; set; }

    public async Task OnGetAsync(string? filter, string? supplier, string? message)
    {
        Message = message;
        Filter = filter ?? "All";
        SupplierSearch = supplier?.Trim();

        var query = db.Payables.AsQueryable();

        query = Filter switch
        {
            "Pending" => query.Where(p => p.Status == PayableStatus.Pending),
            "Partial" => query.Where(p => p.Status == PayableStatus.Partial),
            "Paid"    => query.Where(p => p.Status == PayableStatus.Paid),
            "Overdue" => query.Where(p => p.Status != PayableStatus.Paid && p.DueDate < DateTime.Today),
            _         => query
        };

        if (!string.IsNullOrWhiteSpace(SupplierSearch))
            query = query.Where(p => p.SupplierName.ToLower().Contains(SupplierSearch.ToLower()));

        Payables = await query.OrderBy(p => p.DueDate).ToListAsync();

        var unpaid = await db.Payables
            .Where(p => p.Status != PayableStatus.Paid)
            .ToListAsync();

        TotalPending  = unpaid.Sum(p => p.PendingAmount);
        OverdueCount  = unpaid.Count(p => p.DueDate < DateTime.Today);
        OverdueAmount = unpaid.Where(p => p.DueDate < DateTime.Today).Sum(p => p.PendingAmount);
        DueSoonCount  = unpaid.Count(p => p.DueDate >= DateTime.Today && p.DueDate <= DateTime.Today.AddDays(7));

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        PaidThisMonth = await db.Payables
            .Where(p => p.Status == PayableStatus.Paid && p.CreatedAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(int id, decimal amount)
    {
        var payable = await db.Payables.FindAsync(id);
        if (payable == null) return NotFound();

        payable.PaidAmount = Math.Min(payable.Amount, payable.PaidAmount + amount);
        payable.Status = payable.PaidAmount >= payable.Amount
            ? PayableStatus.Paid
            : PayableStatus.Partial;

        await db.SaveChangesAsync();
        return RedirectToPage(new { message = $"Payment of Rs. {amount:N2} recorded for {payable.SupplierName}." });
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(int id)
    {
        var payable = await db.Payables.FindAsync(id);
        if (payable == null) return NotFound();

        payable.PaidAmount = payable.Amount;
        payable.Status = PayableStatus.Paid;

        await db.SaveChangesAsync();
        return RedirectToPage(new { message = $"{payable.SupplierName} — Rs. {payable.Amount:N2} marked as fully paid." });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var payable = await db.Payables.FindAsync(id);
        if (payable != null)
        {
            db.Payables.Remove(payable);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
