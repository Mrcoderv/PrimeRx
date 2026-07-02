using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Models;

namespace PrimeRx.Pages.Admin.Users;

public class IndexModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : PageModel
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
            UserName = NewStaff.Username,
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
        return RedirectToPage(new { message = "Staff account created successfully." });
    }

    public async Task<IActionResult> OnPostSwitchToStaffAsync(string email)
    {
        if (!User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        var targetUser = await userManager.FindByEmailAsync(email);
        if (targetUser == null)
        {
            ErrorMessage = "User not found.";
            await LoadUsersAsync();
            return Page();
        }

        // Sign out current admin user
        await signInManager.SignOutAsync();

        // Sign in as target staff user
        await signInManager.SignInAsync(targetUser, isPersistent: false);

        // Redirect to Billing page (default for staff)
        return RedirectToPage("/Billing/Index");
    }

    public async Task<IActionResult> OnPostDeleteStaffAsync(string email)
    {
        if (!User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        var targetUser = await userManager.FindByEmailAsync(email);
        if (targetUser == null)
        {
            ErrorMessage = "User not found.";
            await LoadUsersAsync();
            return Page();
        }

        if (await userManager.IsInRoleAsync(targetUser, AppRoles.Admin))
        {
            ErrorMessage = "Cannot delete an Administrator account.";
            await LoadUsersAsync();
            return Page();
        }

        var result = await userManager.DeleteAsync(targetUser);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            await LoadUsersAsync();
            return Page();
        }

        return RedirectToPage(new { message = "Staff account deleted successfully." });
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
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                Roles = roles.ToList()
            });
        }
    }

    public class NewStaffInput
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

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
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }
}