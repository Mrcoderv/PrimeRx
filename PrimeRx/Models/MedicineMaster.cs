using System.ComponentModel.DataAnnotations;

namespace PrimeRx.Models;

public class MedicineMaster
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string GenericName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? BrandName { get; set; }

    [MaxLength(200)]
    public string? Manufacturer { get; set; }

    [MaxLength(50)]
    public string? Form { get; set; }

    [MaxLength(100)]
    public string? Strength { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(20)]
    public string? HSNCode { get; set; }

    [MaxLength(100)]
    public string? RackLocation { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(BrandName)
            ? GenericName
            : $"{BrandName} ({GenericName})";
}
