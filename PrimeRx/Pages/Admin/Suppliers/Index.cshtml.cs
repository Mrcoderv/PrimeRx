using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Data;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Suppliers;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(ApplicationDbContext db) : PageModel
{
    public List<Supplier> Suppliers { get; set; } = [];
    public string? Message { get; set; }
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = "All";

    public async Task OnGetAsync(string? search, string? status, string? message)
    {
        Message = message;
        Search = search ?? string.Empty;
        Status = status ?? "All";

        var query = db.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(term)));
        }

        query = Status switch
        {
            "Active" => query.Where(s => s.IsActive),
            "Inactive" => query.Where(s => !s.IsActive),
            _ => query
        };

        Suppliers = await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            db.Suppliers.Remove(supplier);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { message = "Supplier deleted." });
    }
}
