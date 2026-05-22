using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AdminController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password)
    {
        var admin = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
        
        if (admin != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "AdminCookie");

            await HttpContext.SignInAsync("AdminCookie", new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Error = "Invalid username or password";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalEnquiries = await _context.Enquiries.CountAsync();
        ViewBag.TotalDemoRequests = await _context.DemoRequests.CountAsync();
        return View();
    }

    public async Task<IActionResult> Enquiries()
    {
        var enquiries = await _context.Enquiries.Where(e => e.Status == "Pending" || e.Status == "").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(enquiries);
    }

    public async Task<IActionResult> DemoRequests()
    {
        var requests = await _context.DemoRequests.Where(e => e.Status == "Pending" || e.Status == "").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(requests);
    }

    public async Task<IActionResult> EnquiryDetails(int id)
    {
        var enquiry = await _context.Enquiries.Include(e => e.Notes.OrderByDescending(n => n.CreatedAt)).FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry == null) return NotFound();
        return View(enquiry);
    }

    [HttpPost]
    public async Task<IActionResult> AddEnquiryNote(int id, string noteText)
    {
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var enquiry = await _context.Enquiries.FindAsync(id);
            bool isPost = enquiry?.Status == "Confirmed";
            _context.EnquiryNotes.Add(new EnquiryNote { EnquiryId = id, NoteText = noteText, IsPostConfirmation = isPost });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(EnquiryDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEnquiryStatus(int id, string status)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry != null && (status == "Confirmed" || status == "Rejected"))
        {
            enquiry.Status = status;
            await _context.SaveChangesAsync();
        }
        return status == "Confirmed" ? RedirectToAction(nameof(ConfirmedClients)) : RedirectToAction(nameof(RejectedClients));
    }

    public async Task<IActionResult> DemoRequestDetails(int id)
    {
        var request = await _context.DemoRequests.Include(e => e.Notes.OrderByDescending(n => n.CreatedAt)).FirstOrDefaultAsync(e => e.Id == id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost]
    public async Task<IActionResult> AddDemoRequestNote(int id, string noteText)
    {
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var req = await _context.DemoRequests.FindAsync(id);
            bool isPost = req?.Status == "Confirmed";
            _context.DemoRequestNotes.Add(new DemoRequestNote { DemoRequestId = id, NoteText = noteText, IsPostConfirmation = isPost });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(DemoRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDemoRequestStatus(int id, string status)
    {
        var request = await _context.DemoRequests.FindAsync(id);
        if (request != null && (status == "Confirmed" || status == "Rejected"))
        {
            request.Status = status;
            await _context.SaveChangesAsync();
        }
        return status == "Confirmed" ? RedirectToAction(nameof(ConfirmedClients)) : RedirectToAction(nameof(RejectedClients));
    }

    public async Task<IActionResult> ConfirmedClients()
    {
        ViewBag.Enquiries = await _context.Enquiries.Where(e => e.Status == "Confirmed").OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.DemoRequests = await _context.DemoRequests.Where(e => e.Status == "Confirmed").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View();
    }

    public async Task<IActionResult> RejectedClients()
    {
        ViewBag.Enquiries = await _context.Enquiries.Where(e => e.Status == "Rejected").OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.DemoRequests = await _context.DemoRequests.Where(e => e.Status == "Rejected").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View();
    }

    // --- Settings & SMTP Management ---

    public async Task<IActionResult> Settings()
    {
        var settingsList = await _context.AdminSettings.ToListAsync();
        var settingsDict = settingsList.ToDictionary(s => s.Key, s => s.Value);
        
        // Pass to ViewBag to pre-fill the form
        ViewBag.SmtpHost = settingsDict.ContainsKey("SmtpHost") ? settingsDict["SmtpHost"] : "";
        ViewBag.SmtpPort = settingsDict.ContainsKey("SmtpPort") ? settingsDict["SmtpPort"] : "587";
        ViewBag.SmtpEmail = settingsDict.ContainsKey("SmtpEmail") ? settingsDict["SmtpEmail"] : "";
        ViewBag.SmtpPassword = settingsDict.ContainsKey("SmtpPassword") ? settingsDict["SmtpPassword"] : "";
        ViewBag.SmtpEnableSsl = settingsDict.ContainsKey("SmtpEnableSsl") ? bool.Parse(settingsDict["SmtpEnableSsl"]) : true;

        var adminUser = await _context.AdminUsers.FirstOrDefaultAsync();
        ViewBag.AdminUsername = adminUser?.Username ?? "admin";

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSecurity(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "New passwords do not match!";
            return RedirectToAction("Settings");
        }

        var adminUser = await _context.AdminUsers.FirstOrDefaultAsync();
        if (adminUser != null)
        {
            if (adminUser.PasswordHash != currentPassword)
            {
                TempData["ErrorMessage"] = "Incorrect current password!";
                return RedirectToAction("Settings");
            }

            adminUser.PasswordHash = newPassword;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Security settings updated successfully.";
        }

        return RedirectToAction("Settings");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSmtp(string SmtpHost, string SmtpPort, string SmtpEmail, string SmtpPassword, string SmtpEnableSsl)
    {
        var keysToUpdate = new Dictionary<string, string>
        {
            { "SmtpHost", SmtpHost ?? "" },
            { "SmtpPort", SmtpPort ?? "587" },
            { "SmtpEmail", SmtpEmail ?? "" },
            { "SmtpPassword", SmtpPassword ?? "" },
            { "SmtpEnableSsl", (SmtpEnableSsl == "true" || SmtpEnableSsl == "on").ToString() }
        };

        foreach (var kvp in keysToUpdate)
        {
            var setting = await _context.AdminSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
            if (setting == null)
            {
                _context.AdminSettings.Add(new AdminSetting { Key = kvp.Key, Value = kvp.Value });
            }
            else
            {
                setting.Value = kvp.Value;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "SMTP Email settings updated successfully.";
        
        return RedirectToAction("Settings");
    }

    [HttpPost]
    public async Task<IActionResult> SendEmail(string toEmail, string subject, string message, string returnUrl)
    {
        // Add HTML styling to the email
        string htmlBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f9f9f9; padding: 20px; border-radius: 10px;'>
            <div style='background: linear-gradient(90deg, #048EE7, #00C6FF); padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                <h2 style='color: white; margin: 0;'>Softflip Solutions</h2>
            </div>
            <div style='background-color: white; padding: 30px; border-radius: 0 0 10px 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                <div style='color: #333; font-size: 16px; line-height: 1.6; white-space: pre-wrap;'>{message}</div>
                <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;' />
                <p style='color: #888; font-size: 12px; text-align: center;'>This email was sent by Softflip Solutions.<br>Please do not reply directly to this automated address unless specified.</p>
            </div>
        </div>";

        bool success = await _emailService.SendEmailAsync(toEmail, subject, htmlBody);

        if (success)
        {
            TempData["SuccessMessage"] = $"Email successfully sent to {toEmail}!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to send email. Please check your SMTP settings.";
        }

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Enquiries");
    }
}
