using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Areas.Identity.Pages.Account;

public class VerifyOtpModel(
    UserManager<IdentityUser> userManager,
    OtpStore otpStore) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "OTP Code")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit OTP.")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return RedirectToPage("./ForgotPassword");

        Input.Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var normalized = Input.Email.ToUpperInvariant();

        if (!otpStore.VerifyOtp(normalized, Input.OtpCode))
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired OTP. Please request a new one.");
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user == null)
            return RedirectToPage("./Login");

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        otpStore.StoreVerifiedToken(normalized, resetToken);

        return RedirectToPage("./ResetPassword", new { email = Input.Email });
    }
}
