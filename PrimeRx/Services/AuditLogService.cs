using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Services;

public class AuditLogService(ApplicationDbContext context)
{
    public async Task LogAsync(
        string action,
        string? entityType = null,
        int? entityId = null,
        string? oldValue = null,
        string? newValue = null,
        string? userId = null,
        string? userName = null,
        string? ipAddress = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            Action     = action,
            EntityType = entityType,
            EntityId   = entityId,
            OldValue   = oldValue,
            NewValue   = newValue,
            UserId     = userId,
            UserName   = userName,
            IPAddress  = ipAddress,
            Timestamp  = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentAsync(int count = 100) =>
        await context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();

    public async Task<List<AuditLog>> GetByEntityAsync(string entityType, int entityId) =>
        await context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

    public async Task<List<AuditLog>> GetByUserAsync(string userId, int count = 50) =>
        await context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
}
