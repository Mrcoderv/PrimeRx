using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Data.Seeder;
using PrimeRx.Models;

namespace PrimeRx.Pages.Setup;

public class IndexModel(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    RoleManager<IdentityRole> roleManager) : PageModel
{
    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // If setup is already complete, redirect to Dashboard.
        // Comment out the line below to always show the Setup page during development/testing.
        if (await context.CompanyProfiles.AnyAsync())
            return RedirectToPage("/Dashboard/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Prevent re-running setup if a profile already exists.
        if (await context.CompanyProfiles.AnyAsync())
            return RedirectToPage("/Dashboard/Index");

        await RoleSeeder.SeedAsync(roleManager);

        var company = new CompanyProfile
        {
            Name = Input.CompanyName,
            Address = Input.Address,
            Phone = Input.Phone,
            PAN = Input.PAN,
            GSTIN = Input.GSTIN,
            CreatedAt = DateTime.Now
        };

        context.CompanyProfiles.Add(company);
        await context.SaveChangesAsync();

        var user = new IdentityUser
        {
            UserName = Input.AdminEmail,
            Email = Input.AdminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, Input.AdminPassword);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            // Remove the partially-saved company profile so the user can retry.
            context.CompanyProfiles.Remove(company);
            await context.SaveChangesAsync();
            return Page();
        }

        await userManager.AddToRoleAsync(user, AppRoles.Admin);

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Billing/Index");
    }

    public class SetupInput
    {
        [Required]
        [Display(Name = "Pharmacy Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        public string PAN { get; set; } = string.Empty;
        public string? GSTIN { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Admin Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
