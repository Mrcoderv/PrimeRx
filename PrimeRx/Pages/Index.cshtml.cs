using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;

namespace PrimeRx.Pages;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public bool IsSetupRequired { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Dashboard/Index");

        IsSetupRequired = !await context.CompanyProfiles.AnyAsync();
        return Page();
    }
}
