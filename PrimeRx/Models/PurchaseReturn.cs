using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public static class PurchaseReturnReasons
{
    public const string Expired = "Expired";
    public const string Damaged = "Damaged";
    public const string WrongSupply = "WrongSupply";
    public const string Recall = "Recall";
    public const string Other = "Other";

    public static readonly string[] All = [Expired, Damaged, WrongSupply, Recall, Other];

    public static string Display(string reason) => reason switch
    {
        Expired => "Expired",
        Damaged => "Damaged",
        WrongSupply => "Wrong Supply",
        Recall => "Product Recall",
        Other => "Other",
        _ => reason
    };
}

public class PurchaseReturn
{
    public int Id { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }

    [Required]
    [MaxLength(30)]
    public string Reason { get; set; } = PurchaseReturnReasons.Other;

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PurchaseReturnItem> Items { get; set; } = [];
}
