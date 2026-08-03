using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;

namespace SoftflipSolutions.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage, byte[]? attachment = null, string? attachmentName = null, string category = "General");
}

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyProfileService _companyProfile;
    private readonly IEmailLogService _emailLog;

    public EmailService(ApplicationDbContext context, ICompanyProfileService companyProfile, IEmailLogService emailLog)
    {
        _context = context;
        _companyProfile = companyProfile;
        _emailLog = emailLog;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage, byte[]? attachment = null, string? attachmentName = null, string category = "General")
    {
        try
        {
            var settings = await _context.AdminSettings.ToDictionaryAsync(s => s.Key, s => s.Value);

            if (!settings.ContainsKey("SmtpHost") || string.IsNullOrEmpty(settings["SmtpHost"]))
            {
                await _emailLog.LogAsync(toEmail, subject, category, false, "SMTP not configured");
                return false;
            }

            var host = settings["SmtpHost"];
            var port = int.Parse(settings.ContainsKey("SmtpPort") ? settings["SmtpPort"] : "587");
            var email = settings["SmtpEmail"];
            var password = settings["SmtpPassword"];
            var enableSsl = settings.ContainsKey("SmtpEnableSsl") ? bool.Parse(settings["SmtpEnableSsl"]) : true;
            var company = await _companyProfile.GetAsync();
            var fromName = string.IsNullOrWhiteSpace(company.CompanyName) ? "Softflip Solutions" : company.CompanyName;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            if (attachment != null && attachment.Length > 0)
            {
                mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentName ?? "proposal.pdf", "application/pdf"));
            }

            await client.SendMailAsync(mailMessage);
            await _emailLog.LogAsync(toEmail, subject, category, true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            await _emailLog.LogAsync(toEmail, subject, category, false, ex.Message);
            return false;
        }
    }
}
