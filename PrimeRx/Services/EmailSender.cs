using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace PrimeRx.Services;

public class EmailSender : IEmailSender<IdentityUser>
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

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
        var fromAddress = _config["Email:FromAddress"] ?? "";
        var fromName = _config["Email:FromName"] ?? "PrimeRx";
        var password = _config["Email:Password"] ?? "";
        var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;

        if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(password))
        {
            // Email not configured — skip silently (password reset, etc. won't work)
            return;
        }

        var from = new MailAddress(fromAddress, fromName);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress, password)
        };

        using var message = new MailMessage(from, new MailAddress(email))
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(message);
    }
}
