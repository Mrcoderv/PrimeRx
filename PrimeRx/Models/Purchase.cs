using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Purchase
{
    public int Id { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Now;

    [Required]
    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierPhone { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PurchaseItem> Items { get; set; } = [];
}
