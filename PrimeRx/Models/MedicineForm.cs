using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

/// <summary>
/// Represents a specific form/variant of a medicine.
/// Example: Aspirin exists in Tablet (500mg) and Syrup (100ml) forms.
/// Each form has its own pricing and stock tracking via batches.
/// </summary>
public class MedicineForm
{
    public int Id { get; set; }

    public int MedicineId { get; set; }
    public virtual Medicine Medicine { get; set; } = null!;

    /// <summary>
    /// Form type: Tablet, Syrup, Capsule, Injection, Eye Drop, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FormType { get; set; } = string.Empty;

    /// <summary>
    /// Strength/dose info, e.g., "500mg", "100ml", "2%"
    /// </summary>
    [MaxLength(50)]
    public string? Strength { get; set; }

    /// <summary>
    /// Unit of measure: Pieces, ML, mg, Grams, etc.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = "Pieces";

    /// <summary>
    /// Maximum Retail Price
    /// </summary>
    public decimal MRP { get; set; }

    /// <summary>
    /// Cost price (for profit calculation)
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// Alert threshold for low stock
    /// </summary>
    public int LowStockThreshold { get; set; } = 10;

    /// <summary>
    /// Whether this form is actively sold
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Relations: One MedicineForm has many batches
    public virtual ICollection<MedicineBatch> Batches { get; set; } = [];
}
