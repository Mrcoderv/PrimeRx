using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Company;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public CompanyProfile Company { get; set; } = new();

    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        Company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await context.CompanyProfiles.FirstOrDefaultAsync();
        if (existing is null)
        {
            context.CompanyProfiles.Add(Company);
        }
        else
        {
            existing.Name = Company.Name;
            existing.Address = Company.Address;
            existing.Phone = Company.Phone;
            existing.PAN = Company.PAN;
            existing.GSTIN = Company.GSTIN;
        }

        await context.SaveChangesAsync();
        Message = "Company profile updated.";
        Company = await context.CompanyProfiles.FirstAsync();
        return Page();
    }
}
