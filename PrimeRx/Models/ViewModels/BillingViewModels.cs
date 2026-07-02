using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models.ViewModels;

public class BillLineItem
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? SelectedBatchId { get; set; }

    public decimal LineTotal => (Rate * Quantity) - DiscountAmount;
}

public class CreateBillRequest
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Phone]
    public string CustomerPhone { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = PaymentMethods.Cash;
    public decimal DiscountAmount { get; set; }
    public List<BillLineItem> Items { get; set; } = [];
}

public class MedicineSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public decimal MRP { get; set; }
    public int StockQuantity { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class RecordDuePaymentRequest
{
    public int BillId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = PaymentMethods.Cash;
    public string? Remarks { get; set; }
}

public class PurchaseEntryRequest
{
    public int MedicineId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Bonus/free units received alongside the paid quantity. Added to stock but excluded from the amount charged.</summary>
    public int FreeQuantity { get; set; }
    public string? Reference { get; set; }

    public string? BatchNumber { get; set; }
    public string? PurchaseSource { get; set; }

    /// <summary>Per-unit purchase price for this batch. When greater than zero it updates the medicine's purchase price and recalculates MRP using the configured margin.</summary>
    public decimal? PurchasePrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class StockAdjustmentRequest
{
    public int MedicineId { get; set; }
    public int QuantityChange { get; set; }
    public string? Reference { get; set; }
}
