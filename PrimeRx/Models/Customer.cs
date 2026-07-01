using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

/// <summary>
/// Represents a customer for the pharmacy.
/// Tracks purchase history, loyalty points, and retention metrics.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Total amount spent across all bills (auto-updated on each purchase)
    /// </summary>
    public decimal TotalSpent { get; set; } = 0;

    /// <summary>
    /// Loyalty points accumulated (simple retention feature)
    /// Can be earned at 1 point per ₹100 spent or as rewards
    /// </summary>
    public int LoyaltyPoints { get; set; } = 0;

    /// <summary>
    /// Date of last purchase (used for retention analytics)
    /// </summary>
    public DateTime? LastPurchaseDate { get; set; }

    /// <summary>
    /// When the customer record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether the customer is active/inactive
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Relations
    public virtual ICollection<Bill> Bills { get; set; } = [];
}
