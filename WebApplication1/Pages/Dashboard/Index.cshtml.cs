using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages.Dashboard;

public class IndexModel(DashboardService dashboardService) : PageModel
{
    public DashboardSummary Summary { get; set; } = new();

    public async Task OnGetAsync()
    {
        Summary = await dashboardService.GetSummaryAsync();
    }
}
