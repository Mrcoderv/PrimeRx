using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class InventoryBatch
{
    public int Id { get; set; }

    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    [Required]
    public string BatchNumber { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public string PurchaseSource { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
