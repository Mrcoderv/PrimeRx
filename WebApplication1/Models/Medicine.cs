using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Medicine
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Manufacturer { get; set; }
    public decimal MRP { get; set; }
    public decimal PurchasePrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public DateTime? ExpiryDate { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
}
