namespace PrimeRx.Models;

public class PurchaseReturnItem
{
    public int Id { get; set; }

    public int PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    public string MedicineName { get; set; } = string.Empty;

    public string? BatchNumber { get; set; }

    public int Quantity { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal Amount => Quantity * PurchasePrice;
}
