using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Due;

public class IndexModel(DueService dueService) : PageModel
{
    public List<Bill> DueBills { get; set; } = [];
    public string? Search { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;
        DueBills = await dueService.GetDueBillsAsync(search);
    }
}
