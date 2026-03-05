using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TaskFlowMvc.Services;

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = configuration["Email:Smtp:Host"];
        var portRaw = configuration["Email:Smtp:Port"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromEmail = configuration["Email:Smtp:FromEmail"];
        var fromName = configuration["Email:Smtp:FromName"] ?? "TaskFlow MVC";
        var enableSslRaw = configuration["Email:Smtp:EnableSsl"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portRaw) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            logger.LogWarning("SMTP is not configured. Email to {Email} was skipped. Subject: {Subject}", email, subject);
            return;
        }

        if (!int.TryParse(portRaw, out var port))
        {
            logger.LogWarning("SMTP port is invalid. Email to {Email} was skipped.", email);
            return;
        }

        var enableSsl = true;
        if (bool.TryParse(enableSslRaw, out var parsed))
        {
            enableSsl = parsed;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        mail.To.Add(email);

        await client.SendMailAsync(mail);
    }
}
