using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimeRx.Services;

namespace PrimeRx.Areas.Identity.Pages.Account;

public class ForgotPasswordModel(
    UserManager<IdentityUser> userManager,
    IEmailSender<IdentityUser> emailSender,
    OtpStore otpStore) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        if (user != null)
        {
            var otp = otpStore.GenerateOtp(Input.Email.ToUpperInvariant());
            await emailSender.SendPasswordResetCodeAsync(user, Input.Email, otp);
        }

        return RedirectToPage("./VerifyOtp", new { email = Input.Email });
    }
}
