using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.ViewComponents;

public class CompanyHeaderViewComponent(ApplicationDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var company = await context.CompanyProfiles.AsNoTracking().SingleOrDefaultAsync()
            ?? new CompanyProfile { Name = "PrimeRx" };

        return View(company);
    }
}
