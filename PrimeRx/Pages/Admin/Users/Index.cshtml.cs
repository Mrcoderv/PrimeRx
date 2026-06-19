using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Users;

public class IndexModel(UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public NewStaffInput NewStaff { get; set; } = new();

    public List<StaffUserView> StaffUsers { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? message)
    {
        Message = message;
        await LoadUsersAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = NewStaff.Email,
            Email = NewStaff.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, NewStaff.Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            await LoadUsersAsync();
            return Page();
        }

        await userManager.AddToRoleAsync(user, AppRoles.Staff);
        return RedirectToPage(new { message = "Staff account created." });
    }

    private async Task LoadUsersAsync()
    {
        var users = await userManager.GetUsersInRoleAsync(AppRoles.Staff);
        StaffUsers = [];

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            StaffUsers.Add(new StaffUserView
            {
                Email = user.Email ?? user.UserName ?? "",
                Roles = roles.ToList()
            });
        }
    }

    public class NewStaffInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class StaffUserView
    {
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }
}
