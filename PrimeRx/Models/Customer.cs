using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Customer Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Retention metrics
    [Display(Name = "Total Amount Spent")]
    public decimal TotalSpent { get; set; } = 0;

    [Display(Name = "Loyalty Points")]
    public int LoyaltyPoints { get; set; } = 0;

    [Display(Name = "Last Purchase Date")]
    public DateTime? LastPurchaseDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    // Relations
    public ICollection<Bill> Bills { get; set; } = [];
}
