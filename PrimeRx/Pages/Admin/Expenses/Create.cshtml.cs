using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;
using PrimeRx.Services;

namespace PrimeRx.Pages.Admin.Expenses;

[Authorize(Policy = "AdminOnly")]
public class CreateModel(ExpenseService expenseService, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public Expense Expense { get; set; } = new();

    public List<string> StaffEmails { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadStaffAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStaffAsync();
            return Page();
        }

        Expense.CreatedBy = User.Identity?.Name;
        await expenseService.AddExpenseAsync(Expense);
        return RedirectToPage("/Admin/Expenses/Index", new { message = "Expense recorded." });
    }

    private async Task LoadStaffAsync()
    {
        var staff = await userManager.GetUsersInRoleAsync(AppRoles.Staff);
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        StaffEmails = staff.Concat(admins)
            .Select(u => u.Email ?? u.UserName ?? "")
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .OrderBy(e => e)
            .ToList();
    }
}
