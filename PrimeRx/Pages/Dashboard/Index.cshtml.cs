using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Pages.Dashboard;

public class IndexModel(DashboardService dashboardService) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DashboardSummary Summary { get; set; } = new();
    public string SalesTrendJson { get; private set; } = "[]";
    public string MedicineChartJson { get; private set; } = "[]";

    public async Task OnGetAsync()
    {
        Summary = await dashboardService.GetSummaryAsync();
        SalesTrendJson = JsonSerializer.Serialize(Summary.SalesTrend, JsonOptions);
        MedicineChartJson = JsonSerializer.Serialize(Summary.TopMedicines, JsonOptions);
    }
}
