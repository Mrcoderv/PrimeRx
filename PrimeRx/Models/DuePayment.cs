namespace PrimeRx.Models;

public class DuePayment
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}
