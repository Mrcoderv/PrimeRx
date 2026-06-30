using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models.ViewModels;

public class PurchaseLineItem
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal MRP { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Amount => Quantity * PurchasePrice;
}

public class PurchaseCreateRequest
{
    [Required(ErrorMessage = "Supplier name is required")]
    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierPhone { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    public string? Notes { get; set; }

    public List<PurchaseLineItem> Items { get; set; } = [];
}
