using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

/// <summary>
/// Represents a specific batch of a medicine form.
/// Tracks batch number, quantity, expiry date, and purchase source.
/// Each batch is linked to a MedicineForm (not directly to Medicine).
/// </summary>
public class InventoryBatch
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to MedicineForm (the variant being stocked)
    /// </summary>
    public int MedicineFormId { get; set; }
    public virtual MedicineForm MedicineForm { get; set; } = null!;

    /// <summary>
    /// For backward compatibility: also keep MedicineId to locate the parent medicine
    /// </summary>
    public int MedicineId { get; set; }
    public virtual Medicine Medicine { get; set; } = null!;

    [Required]
    public string BatchNumber { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public string PurchaseSource { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
