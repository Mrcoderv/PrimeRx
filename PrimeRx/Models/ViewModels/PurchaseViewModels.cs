using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models.ViewModels;

public class PurchaseLineItem
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int FreeQuantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal MRP { get; set; }
    public decimal ConversionCharge { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? GenericName { get; set; }
    public string? Manufacturer { get; set; }
    public string? FormType { get; set; }
    public string? Strength { get; set; }
    public string? Unit { get; set; }
    public decimal Amount => Math.Round(Quantity * PurchasePrice * (1 - DiscountPercent / 100m), 2);
}

public class PurchaseCreateRequest
{
    [Required(ErrorMessage = "Supplier name is required")]
    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierPhone { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    public string? Notes { get; set; }

    public string PaymentType { get; set; } = "Cash";

    public int? CreditDays { get; set; }

    public List<PurchaseLineItem> Items { get; set; } = [];
}

public class PayableAgingRow
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? InvoiceNo { get; set; }
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount => Amount - PaidAmount;
    public int AgeDays { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PayableAgingReport
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string Title { get; set; } = "Ageing Dues";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string? SupplierFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<PayableAgingRow> Rows { get; set; } = [];
    public decimal GrandTotal => Rows.Sum(r => r.PendingAmount);
    public int TotalCount => Rows.Count;
}
