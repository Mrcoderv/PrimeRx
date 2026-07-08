namespace PrimeRx.Models.ViewModels;

public class AgingDueRow
{
    public string PartyName { get; set; } = string.Empty;
    public string PartyType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Narration { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => Amount - PaidAmount;
    public int AgeDays { get; set; }
}

public class AgingDueReport
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string Title { get; set; } = "Ageing Dues";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string? PartyFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? AsOnDate { get; set; }
    public List<AgingDueRow> SupplierRows { get; set; } = [];
    public List<AgingDueRow> CustomerRows { get; set; } = [];
    public decimal SupplierTotal => SupplierRows.Sum(r => r.Balance);
    public decimal CustomerTotal => CustomerRows.Sum(r => r.Balance);
    public decimal GrandTotal => SupplierTotal + CustomerTotal;
    public int TotalCount => SupplierRows.Count + CustomerRows.Count;
}
