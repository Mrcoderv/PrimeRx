using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrimeRx.Pages.Admin.Company;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/Settings/Index");
    public IActionResult OnPost() => RedirectToPage("/Admin/Settings/Index");
}
