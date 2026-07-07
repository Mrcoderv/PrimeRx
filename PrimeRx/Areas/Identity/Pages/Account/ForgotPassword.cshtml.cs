using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrimeRx.Areas.Identity.Pages.Account;

public class ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender<IdentityUser> emailSender) : PageModel
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
        if (user == null)
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { email = Input.Email, code },
            protocol: Request.Scheme)!;

        await emailSender.SendPasswordResetLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
