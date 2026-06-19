using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.ViewComponents;

public class CompanyHeaderViewComponent(ApplicationDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var company = await context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync()
            ?? new CompanyProfile { Name = "PrimeRx" };

        return View(company);
    }
}
