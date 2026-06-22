using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Settings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public CompanyProfile Settings { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; } = "company";

    public string? Message { get; set; }

    public async Task OnGetAsync(string? message)
    {
        Message = message;
        Settings = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Tab = "company";
            return Page();
        }

        var existing = await context.CompanyProfiles.FirstOrDefaultAsync();
        if (existing is null)
        {
            context.CompanyProfiles.Add(Settings);
        }
        else
        {
            existing.Name = Settings.Name;
            existing.Address = Settings.Address;
            existing.Phone = Settings.Phone;
            existing.PAN = Settings.PAN;
            existing.GSTIN = Settings.GSTIN;
            existing.TaxRate = Settings.TaxRate;
            existing.TaxLabel = Settings.TaxLabel;
            existing.TaxInclusive = Settings.TaxInclusive;
            existing.BillTitle = Settings.BillTitle;
            existing.BillFooterText = Settings.BillFooterText;
            existing.BillPrimaryColor = Settings.BillPrimaryColor;
            existing.ShowPanOnBill = Settings.ShowPanOnBill;
            existing.ShowGstinOnBill = Settings.ShowGstinOnBill;
            existing.DefaultDiscountMarginPercent = Settings.DefaultDiscountMarginPercent;
        }

        await context.SaveChangesAsync();
        return RedirectToPage(new { message = "Settings saved successfully.", tab = Tab ?? "company" });
    }
}
