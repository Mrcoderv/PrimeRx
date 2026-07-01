using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Purchase;

public class SupplierModel(PurchaseService purchaseService, ApplicationDbContext db) : PageModel
{
    public string SupplierName { get; set; } = string.Empty;
    public Supplier? SupplierProfile { get; set; }
    public List<PrimeRx.Models.Purchase> Purchases { get; set; } = [];
    public List<Payable> Payables { get; set; } = [];

    // Summary stats
    public decimal TotalSpend { get; set; }
    public decimal AvgInvoice { get; set; }
    public int TotalItems { get; set; }
    public decimal PendingPayable { get; set; }
    public int OverdueCount { get; set; }

    // Top medicines
    public List<(string Name, int Qty, decimal Spend)> TopMedicines { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToPage("/Purchase/Index");

        SupplierName = name.Trim();

        // Load purchases
        Purchases = await purchaseService.GetBySupplierAsync(SupplierName);

        // Load supplier profile if exists in Suppliers table
        SupplierProfile = await db.Suppliers
            .FirstOrDefaultAsync(s => s.Name == SupplierName && s.IsActive);

        // Load payables for this supplier
        Payables = await db.Payables
            .Where(p => p.SupplierName.ToLower() == SupplierName.ToLower())
            .OrderByDescending(p => p.DueDate)
            .ToListAsync();

        // Compute stats
        TotalSpend = Purchases.Sum(p => p.TotalAmount);
        AvgInvoice = Purchases.Count > 0 ? TotalSpend / Purchases.Count : 0;
        TotalItems = Purchases.SelectMany(p => p.Items).Sum(i => i.Quantity);

        var unpaidPayables = Payables.Where(p => p.Status != PayableStatus.Paid).ToList();
        PendingPayable = unpaidPayables.Sum(p => p.PendingAmount);
        OverdueCount = unpaidPayables.Count(p => p.IsOverdue);

        // Top medicines by spend
        TopMedicines = Purchases
            .SelectMany(p => p.Items)
            .GroupBy(i => i.MedicineName)
            .Select(g => (Name: g.Key, Qty: g.Sum(i => i.Quantity), Spend: g.Sum(i => i.Amount)))
            .OrderByDescending(x => x.Spend)
            .Take(10)
            .ToList();

        return Page();
    }
}
