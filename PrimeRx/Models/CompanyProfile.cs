namespace PrimeRx.Models;

public class CompanyProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string? GSTIN { get; set; }
    public string? LogoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Tax settings
    public decimal TaxRate { get; set; }
    public string TaxLabel { get; set; } = "GST";
    public bool TaxInclusive { get; set; }

    // Bill design
    public string BillTitle { get; set; } = "Tax Invoice";
    public string? BillFooterText { get; set; }
    public string BillPrimaryColor { get; set; } = "#2563eb";
    public bool ShowPanOnBill { get; set; } = true;
    public bool ShowGstinOnBill { get; set; } = true;
}
