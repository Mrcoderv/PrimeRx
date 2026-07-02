using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Medicine
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Manufacturer { get; set; }
    public decimal MRP { get; set; }
    public decimal PurchasePrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public DateTime? ExpiryDate { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>Form type: Tablet, Syrup, Capsule, Injection, Eye Drop, etc.</summary>
    public string? FormType { get; set; }

    // Latest batch / supplier recorded for this medicine (see InventoryBatch for full history).
    public string? BatchNumber { get; set; }
    public string? PurchaseSource { get; set; }
}
