using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class AuditLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    [MaxLength(100)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [MaxLength(45)]
    public string? IPAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
