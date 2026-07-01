using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Bill
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; } = DateTime.Now;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Foreign key to Customer entity (optional for guest purchases)
    /// </summary>
    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = PaymentMethods.Cash;
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string PaymentStatus { get; set; } = PaymentStatuses.Paid;
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }

    public List<SaleItem> SaleItems { get; set; } = [];
    public List<DuePayment> DuePayments { get; set; } = [];
}
