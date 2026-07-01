using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Suppliers;

[Authorize(Policy = "AdminOnly")]
public class AddModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public SupplierInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var supplier = new Supplier
            {
                Name = Input.Name.Trim(),
                PAN = Input.PAN?.Trim(),
                DdaRegNo = Input.DdaRegNo?.Trim(),
                Address = Input.Address?.Trim(),
                Phone = Input.Phone?.Trim(),
                Email = Input.Email?.Trim(),
                CreditDays = Input.CreditDays,
                ContactPerson = Input.ContactPerson?.Trim(),
                IsActive = Input.IsActive,
                Notes = Input.Notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();

            return RedirectToPage("/Admin/Suppliers/Index", new { message = $"Supplier '{supplier.Name}' added successfully." });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class SupplierInput
    {
        [Required(ErrorMessage = "Supplier name is required")]
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

        [Range(0, 365)]
        [Display(Name = "Credit Days")]
        public int CreditDays { get; set; } = 30;

        [MaxLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }
    }
}
