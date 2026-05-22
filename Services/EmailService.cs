using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;

namespace SoftflipSolutions.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage);
}

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;

    public EmailService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        try
        {
            var settings = await _context.AdminSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
            
            if (!settings.ContainsKey("SmtpHost") || string.IsNullOrEmpty(settings["SmtpHost"]))
            {
                return false; // SMTP not configured
            }

            var host = settings["SmtpHost"];
            var port = int.Parse(settings.ContainsKey("SmtpPort") ? settings["SmtpPort"] : "587");
            var email = settings["SmtpEmail"];
            var password = settings["SmtpPassword"];
            var enableSsl = settings.ContainsKey("SmtpEnableSsl") ? bool.Parse(settings["SmtpEnableSsl"]) : true;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email, "SoftflipSolutions"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            return false;
        }
    }
}
