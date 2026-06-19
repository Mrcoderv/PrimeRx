namespace WebApplication1.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPerItem { get; set; }
    public decimal Amount { get; set; }
}
