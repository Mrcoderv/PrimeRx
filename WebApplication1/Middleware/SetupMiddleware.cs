using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Middleware;

public class SetupMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var isSetupPath = path.StartsWith("/Setup", StringComparison.OrdinalIgnoreCase);
        var isErrorPath = path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase);
        var isHomePath = path == "/" || path.Equals("/Index", StringComparison.OrdinalIgnoreCase);
        var isStatic = path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_", StringComparison.OrdinalIgnoreCase);

        if (!isSetupPath && !isErrorPath && !isHomePath && !isStatic)
        {
            var isConfigured = await dbContext.CompanyProfiles.AnyAsync();
            if (!isConfigured)
            {
                context.Response.Redirect("/Setup");
                return;
            }
        }

        await next(context);
    }
}
