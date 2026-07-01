using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Data;

namespace PrimeRx.Pages.Admin.Suppliers;

[Authorize(Policy = "AdminOnly")]
public class EditModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public SupplierInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        Input = new SupplierInput
        {
            Id = supplier.Id,
            Name = supplier.Name,
            PAN = supplier.PAN,
            DdaRegNo = supplier.DdaRegNo,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            CreditDays = supplier.CreditDays,
            ContactPerson = supplier.ContactPerson,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var supplier = await db.Suppliers.FindAsync(Input.Id);
            if (supplier == null) return NotFound();

            supplier.Name = Input.Name.Trim();
            supplier.PAN = Input.PAN?.Trim();
            supplier.DdaRegNo = Input.DdaRegNo?.Trim();
            supplier.Address = Input.Address?.Trim();
            supplier.Phone = Input.Phone?.Trim();
            supplier.Email = Input.Email?.Trim();
            supplier.CreditDays = Input.CreditDays;
            supplier.ContactPerson = Input.ContactPerson?.Trim();
            supplier.IsActive = Input.IsActive;
            supplier.Notes = Input.Notes?.Trim();

            await db.SaveChangesAsync();

            return RedirectToPage("/Admin/Suppliers/Index", new { message = $"Supplier '{supplier.Name}' updated." });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class SupplierInput
    {
        public int Id { get; set; }

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
