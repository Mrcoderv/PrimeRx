using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public static class CreditNoteStatus
{
    public const string Available = "Available";
    public const string PartiallyUsed = "PartiallyUsed";
    public const string FullyUsed = "FullyUsed";
}

public class CreditNote
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    public int PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal UsedAmount { get; set; }

    public string Status { get; set; } = CreditNoteStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal AvailableAmount => Amount - UsedAmount;
}
