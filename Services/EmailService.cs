using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;

namespace SoftflipSolutions.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage, byte[]? attachment = null, string? attachmentName = null, string category = "General");
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage, IEnumerable<(byte[] Content, string FileName, string ContentType)>? attachments, string category = "General");
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
        var attachments = new List<(byte[] Content, string FileName, string ContentType)>();
        if (attachment != null && attachment.Length > 0)
        {
            var name = attachmentName ?? "proposal.pdf";
            var contentType = name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream";
            attachments.Add((attachment, name, contentType));
        }
        return await SendEmailAsync(toEmail, subject, htmlMessage, attachments, category);
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlMessage,
        IEnumerable<(byte[] Content, string FileName, string ContentType)>? attachments,
        string category = "General")
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
                From = new MailAddress(email, fromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            mailMessage.To.Add(toEmail);

            // AlternateView + explicit charset so emoji / Unicode survive SMTP transfer.
            var htmlView = AlternateView.CreateAlternateViewFromString(
                htmlMessage, Encoding.UTF8, MediaTypeNames.Text.Html);
            htmlView.ContentType.CharSet = "utf-8";
            mailMessage.AlternateViews.Add(htmlView);

            var streams = new List<MemoryStream>();
            try
            {
                if (attachments != null)
                {
                    foreach (var (content, fileName, contentType) in attachments)
                    {
                        if (content == null || content.Length == 0) continue;
                        var ms = new MemoryStream(content);
                        streams.Add(ms);
                        mailMessage.Attachments.Add(new Attachment(ms, fileName, contentType));
                    }
                }

                await client.SendMailAsync(mailMessage);
            }
            finally
            {
                htmlView.Dispose();
                foreach (var s in streams) s.Dispose();
            }

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
