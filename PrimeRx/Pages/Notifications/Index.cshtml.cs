using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly DueService _dueService;
    private readonly InventoryService _inventoryService;
    private readonly ApplicationDbContext _db;

    public IndexModel(DueService dueService, InventoryService inventoryService, ApplicationDbContext db)
    {
        _dueService = dueService;
        _inventoryService = inventoryService;
        _db = db;
    }

    public List<Bill> CustomerDues { get; set; } = new();
    public List<Medicine> Expiring { get; set; } = new();
    public List<Medicine> LowStock { get; set; } = new();
    public List<Payable> PendingPayables { get; set; } = new();

    public int TotalDueAmount { get; set; }
    public int TotalExpiringCount { get; set; }
    public int TotalLowStockCount { get; set; }

    public async Task OnGetAsync()
    {
        CustomerDues = await _dueService.GetDueBillsAsync();
        Expiring = await _inventoryService.GetExpiringMedicinesAsync(30);
        LowStock = await _inventoryService.GetLowStockAsync();
        PendingPayables = await _db.Payables
            .Where(p => p.Status != PayableStatus.Paid)
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        TotalDueAmount = CustomerDues.Count;
        TotalExpiringCount = Expiring.Count;
        TotalLowStockCount = LowStock.Count;
    }

    // Called by the navbar bell dropdown via fetch
    public async Task<JsonResult> OnGetBellSummaryAsync()
    {
        var customerDueCount = (await _dueService.GetDueBillsAsync()).Count;
        var expiringCount = (await _inventoryService.GetExpiringMedicinesAsync(30)).Count;
        var lowStockCount = (await _inventoryService.GetLowStockAsync()).Count;

        var payablesDue = await _db.Payables
            .Where(p => p.Status != PayableStatus.Paid && p.DueDate <= DateTime.Today.AddDays(7))
            .OrderBy(p => p.DueDate)
            .Take(5)
            .Select(p => new
            {
                supplierName = p.SupplierName,
                invoiceNo = p.InvoiceNo,
                dueDate = p.DueDate,
                pendingAmount = p.Amount - p.PaidAmount,
                isOverdue = p.DueDate < DateTime.Today
            })
            .ToListAsync();

        return new JsonResult(new
        {
            totalCount = customerDueCount + expiringCount + lowStockCount + payablesDue.Count,
            customerDues = customerDueCount,
            expiring = expiringCount,
            lowStock = lowStockCount,
            payables = payablesDue
        });
    }
}
