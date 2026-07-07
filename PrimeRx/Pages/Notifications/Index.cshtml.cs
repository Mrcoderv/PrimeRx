using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Notifications;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly DueService _dueService;
    private readonly InventoryService _inventoryService;
    private readonly NotificationService _notificationService;
    private readonly ApplicationDbContext _db;

    public IndexModel(DueService dueService, InventoryService inventoryService, NotificationService notificationService, ApplicationDbContext db)
    {
        _dueService = dueService;
        _inventoryService = inventoryService;
        _notificationService = notificationService;
        _db = db;
    }

    public List<Bill> CustomerDues { get; set; } = new();
    public List<Medicine> Expiring { get; set; } = new();
    public List<Medicine> LowStock { get; set; } = new();
    public List<Payable> PendingPayables { get; set; } = new();
    public List<Notification> NotificationItems { get; set; } = new();

    public int TotalDueAmount { get; set; }
    public int TotalExpiringCount { get; set; }
    public int TotalLowStockCount { get; set; }
    public int UnreadCount { get; set; }

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

        NotificationItems = await _notificationService.GetNotificationsAsync();
        UnreadCount = await _notificationService.GetUnreadCountAsync();
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

    public async Task<JsonResult> OnGetNotificationsJsonAsync()
    {
        var items = await _notificationService.GetNotificationsAsync(20);
        var unreadCount = await _notificationService.GetUnreadCountAsync();

            return new JsonResult(new
            {
                unreadCount,
                notifications = items.Select(n => new
                {
                    n.Id,
                    n.Type,
                    refType = n.ReferenceType,
                    refId = n.ReferenceId,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.IsActionCompleted,
                    n.CreatedAt,
                    timeAgo = FormatTimeAgo(n.CreatedAt)
                })
            });
    }

    public async Task<JsonResult> OnPostMarkReadAsync([FromBody] MarkRequest req)
    {
        await _notificationService.MarkAsReadAsync(req.Id);
        return new JsonResult(new { ok = true });
    }

    public async Task<JsonResult> OnPostMarkAllReadAsync()
    {
        await _notificationService.MarkAllAsReadAsync();
        return new JsonResult(new { ok = true });
    }

    public async Task<JsonResult> OnPostDismissAsync([FromBody] MarkRequest req)
    {
        await _notificationService.DismissAsync(req.Id);
        return new JsonResult(new { ok = true });
    }

    public async Task<JsonResult> OnPostDismissAllCompletedAsync()
    {
        await _notificationService.DismissAllCompletedAsync();
        return new JsonResult(new { ok = true });
    }

    public static string FormatTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return dt.ToString("dd-MMM-yyyy");
    }
}

public class MarkRequest
{
    public int Id { get; set; }
}
