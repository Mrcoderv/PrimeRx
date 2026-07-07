namespace PrimeRx.Models;

public class Notification
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public bool IsActionCompleted { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public static class NotificationTypes
{
    public const string DueExpiry = "DueExpiry";
    public const string ExpiringMedicine = "ExpiringMedicine";
    public const string LowStock = "LowStock";
    public const string PayableDue = "PayableDue";
}
