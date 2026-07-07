using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrimeRx.Areas.Identity.Pages.Account;

public class ResetPasswordModel(UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? email = null, string? code = null)
    {
        if (code == null)
        {
            return BadRequest("A code must be supplied for password reset.");
        }

        Input.Code = code;
        Input.Email = email ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            return RedirectToPage("./Login");
        }

        var result = await userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("./Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
