using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

public class NotificationService(ApplicationDbContext db)
{
    public async Task<List<Notification>> GetNotificationsAsync(int count = 50)
    {
        await SyncNotificationsAsync();

        return await db.Notifications
            .Where(n => !n.IsDismissed)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        await SyncNotificationsAsync();

        return await db.Notifications
            .Where(n => !n.IsRead && !n.IsDismissed)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n is not null && !n.IsRead)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        var unread = await db.Notifications
            .Where(n => !n.IsRead && !n.IsDismissed)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task DismissAsync(int id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n is not null && !n.IsDismissed)
        {
            n.IsDismissed = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task DismissAllCompletedAsync()
    {
        var completed = await db.Notifications
            .Where(n => n.IsActionCompleted && !n.IsDismissed)
            .ToListAsync();

        foreach (var n in completed)
            n.IsDismissed = true;

        await db.SaveChangesAsync();
    }

    public async Task MarkActionCompletedAsync(string referenceType, int referenceId)
    {
        var notifications = await db.Notifications
            .Where(n => n.ReferenceType == referenceType && n.ReferenceId == referenceId && !n.IsActionCompleted)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in notifications)
        {
            n.IsActionCompleted = true;
            n.CompletedAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task SyncNotificationsAsync()
    {
        var now = DateTime.UtcNow;

        // --- Sync Due Bills ---
        var dues = await db.Bills
            .Include(b => b.DuePayments)
            .Where(b => b.DueAmount > 0)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();

        var dueIds = dues.Select(d => d.Id).ToHashSet();
        var existingDueNots = await db.Notifications
            .Where(n => n.ReferenceType == "Bill")
            .ToListAsync();

        foreach (var not in existingDueNots)
        {
            if (!dueIds.Contains(not.ReferenceId))
            {
                not.IsActionCompleted = true;
                not.CompletedAt ??= now;
            }
        }

        foreach (var bill in dues)
        {
            var exists = existingDueNots.Any(n =>
                n.ReferenceId == bill.Id &&
                !n.IsActionCompleted &&
                !n.IsDismissed);

            if (!exists)
            {
                db.Notifications.Add(new Notification
                {
                    Type = NotificationTypes.DueExpiry,
                    ReferenceType = "Bill",
                    ReferenceId = bill.Id,
                    Title = $"Due Collection — {bill.CustomerName}",
                    Message = $"Bill #{bill.BillNumber}: Rs. {bill.DueAmount:N2} pending since {bill.BillDate:dd-MMM-yyyy}",
                    CreatedAt = now
                });
            }
        }

        // --- Sync Expiring Medicines ---
        var today = DateTime.Today;
        var expiring = await db.Medicines
            .Where(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value <= today.AddDays(30))
            .OrderBy(m => m.ExpiryDate)
            .ToListAsync();

        var expiringIds = expiring.Select(m => m.Id).ToHashSet();
        var existingExpNots = await db.Notifications
            .Where(n => n.ReferenceType == "Medicine" && n.Type == NotificationTypes.ExpiringMedicine)
            .ToListAsync();

        foreach (var not in existingExpNots)
        {
            if (!expiringIds.Contains(not.ReferenceId))
            {
                not.IsActionCompleted = true;
                not.CompletedAt ??= now;
            }
        }

        foreach (var med in expiring)
        {
            var exists = existingExpNots.Any(n =>
                n.ReferenceId == med.Id &&
                !n.IsActionCompleted &&
                !n.IsDismissed);

            if (!exists)
            {
                var daysLeft = (med.ExpiryDate!.Value - today).Days;
                db.Notifications.Add(new Notification
                {
                    Type = NotificationTypes.ExpiringMedicine,
                    ReferenceType = "Medicine",
                    ReferenceId = med.Id,
                    Title = $"Expiring Soon — {med.Name}",
                    Message = $"{daysLeft} days left (Expires: {med.ExpiryDate:dd-MMM-yyyy})",
                    CreatedAt = now
                });
            }
        }

        // --- Sync Low Stock ---
        var lowStock = await db.Medicines
            .Where(m => m.IsActive && m.StockQuantity <= m.LowStockThreshold)
            .OrderBy(m => m.StockQuantity)
            .ToListAsync();

        var lowStockIds = lowStock.Select(m => m.Id).ToHashSet();
        var existingLowNots = await db.Notifications
            .Where(n => n.ReferenceType == "Medicine" && n.Type == NotificationTypes.LowStock)
            .ToListAsync();

        foreach (var not in existingLowNots)
        {
            if (!lowStockIds.Contains(not.ReferenceId))
            {
                not.IsActionCompleted = true;
                not.CompletedAt ??= now;
            }
        }

        foreach (var med in lowStock)
        {
            var exists = existingLowNots.Any(n =>
                n.ReferenceId == med.Id &&
                !n.IsActionCompleted &&
                !n.IsDismissed);

            if (!exists)
            {
                db.Notifications.Add(new Notification
                {
                    Type = NotificationTypes.LowStock,
                    ReferenceType = "Medicine",
                    ReferenceId = med.Id,
                    Title = $"Low Stock — {med.Name}",
                    Message = $"Only {med.StockQuantity} units left (threshold: {med.LowStockThreshold})",
                    CreatedAt = now
                });
            }
        }

        // --- Sync Payables ---
        var payables = await db.Payables
            .Where(p => p.Status != PayableStatus.Paid)
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var payableIds = payables.Select(p => p.Id).ToHashSet();
        var existingPayNots = await db.Notifications
            .Where(n => n.ReferenceType == "Payable")
            .ToListAsync();

        foreach (var not in existingPayNots)
        {
            if (!payableIds.Contains(not.ReferenceId))
            {
                not.IsActionCompleted = true;
                not.CompletedAt ??= now;
            }
        }

        foreach (var p in payables)
        {
            var exists = existingPayNots.Any(n =>
                n.ReferenceId == p.Id &&
                !n.IsActionCompleted &&
                !n.IsDismissed);

            if (!exists)
            {
                var overdue = p.DueDate < today;
                db.Notifications.Add(new Notification
                {
                    Type = NotificationTypes.PayableDue,
                    ReferenceType = "Payable",
                    ReferenceId = p.Id,
                    Title = $"Payable Due — {p.SupplierName}",
                    Message = $"Rs. {p.PendingAmount:N2} {(overdue ? "OVERDUE" : "due")} by {p.DueDate:dd-MMM-yyyy}" +
                              (!string.IsNullOrEmpty(p.InvoiceNo) ? $" (Invoice: {p.InvoiceNo})" : ""),
                    CreatedAt = now
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
