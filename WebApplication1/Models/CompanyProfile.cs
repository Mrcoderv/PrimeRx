namespace WebApplication1.Models;

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
}
