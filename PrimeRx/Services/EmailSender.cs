using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace PrimeRx.Services;

public class EmailSender : IEmailSender<IdentityUser>
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await SendMailAsync(email, subject, htmlMessage);
    }

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        return SendMailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink)
    {
        return SendMailAsync(email, "Reset Password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        return SendMailAsync(email, "Password Reset Code",
            $"Your password reset code is: {resetCode}");
    }

    private async Task SendMailAsync(string email, string subject, string htmlMessage)
    {
        var fromAddress = new MailAddress("raghavap.339@gmail.com", "PrimeRx");
        const string fromPassword = "yxht jwla vjkr idto";

        using var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using var message = new MailMessage(fromAddress, new MailAddress(email))
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(message);
    }
}
