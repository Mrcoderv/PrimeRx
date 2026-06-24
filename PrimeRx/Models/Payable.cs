using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Payable
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? InvoiceNo { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; } = 0;

    [Required]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    public string Status { get; set; } = PayableStatus.Pending;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal PendingAmount => Amount - PaidAmount;

    public bool IsOverdue => Status != PayableStatus.Paid && DueDate < DateTime.Today;
}
