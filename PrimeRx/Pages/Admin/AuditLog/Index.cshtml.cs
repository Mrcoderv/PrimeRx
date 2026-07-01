using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.AuditLog;

public class IndexModel(AuditLogService auditLogService) : PageModel
{
    public List<Models.AuditLog> Logs { get; set; } = [];

    public async Task OnGetAsync()
    {
        Logs = await auditLogService.GetRecentAsync(200);
    }
}
