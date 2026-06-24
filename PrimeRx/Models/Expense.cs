using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Expense
{
    public int Id { get; set; }

    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public string Category { get; set; } = ExpenseCategories.Miscellaneous;

    public string? Reason { get; set; }

    // Optional staff member associated with the expense (e.g. salary recipient).
    public string? StaffId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Audit fields
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
