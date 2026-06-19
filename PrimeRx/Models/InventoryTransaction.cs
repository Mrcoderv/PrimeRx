namespace PrimeRx.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public string? Reference { get; set; }
}
