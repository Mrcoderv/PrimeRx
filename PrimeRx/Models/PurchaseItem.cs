using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class PurchaseItem
{
    public int Id { get; set; }

    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    [Required]
    public string MedicineName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal MRP { get; set; }

    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int FreeQuantity { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal ConversionCharge { get; set; }

    public decimal Amount => Math.Round(Quantity * PurchasePrice * (1 - DiscountPercent / 100m), 2);
}
