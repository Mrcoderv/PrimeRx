using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Supplier / Company Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "PAN Number")]
    public string? PAN { get; set; }

    [MaxLength(100)]
    [Display(Name = "DDA Reg. No.")]
    public string? DdaRegNo { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Credit Days")]
    [Range(0, 365)]
    public int CreditDays { get; set; } = 30;

    [MaxLength(100)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
