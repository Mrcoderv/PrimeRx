namespace PrimeRx.Models;

/// <summary>
/// Represents one line item on a bill.
/// Tracks which batch was sold for traceability.
/// </summary>
public class SaleItem
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public virtual Bill Bill { get; set; } = null!;

    public int MedicineId { get; set; }
    public virtual Medicine Medicine { get; set; } = null!;

    /// <summary>
    /// Reference to the batch sold (for traceability)
    /// </summary>
    public int? BatchId { get; set; }
    public virtual InventoryBatch? Batch { get; set; }

    public string MedicineName { get; set; } = string.Empty;
    public string? PackSize { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal MRP { get; set; }
    public decimal Rate { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
}
