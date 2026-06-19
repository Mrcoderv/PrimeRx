using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Billing;

public class HistoryModel(ApplicationDbContext context) : PageModel
{
    public List<Bill> Bills { get; set; } = [];
    public string? Search { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;
        var query = context.Bills.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.BillNumber.ToLower().Contains(term) ||
                b.CustomerName.ToLower().Contains(term) ||
                b.CustomerPhone.Contains(term));
        }

        Bills = await query.OrderByDescending(b => b.BillDate).Take(100).ToListAsync();
    }
}
