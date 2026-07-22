using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;
using SoftflipSolutions.ViewModels;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDealPdfService _dealPdfService;
    private readonly ICompanyProfileService _companyProfile;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        ApplicationDbContext context,
        IEmailService emailService,
        IDealPdfService dealPdfService,
        ICompanyProfileService companyProfile,
        IWebHostEnvironment env)
    {
        _context = context;
        _emailService = emailService;
        _dealPdfService = dealPdfService;
        _companyProfile = companyProfile;
        _env = env;
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

    public static readonly string[] DefaultLeadSources = ["WhatsApp", "Just Dial", "IndiaMART"];

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalEnquiries = await _context.Enquiries.CountAsync();
        ViewBag.TotalDemoRequests = await _context.DemoRequests.CountAsync();
        ViewBag.TotalClientLeads = await _context.ClientLeads.CountAsync();
        ViewBag.TotalPartners = await _context.ChannelPartners.CountAsync();
        ViewBag.TotalPartnerClients = await _context.PartnerClients.CountAsync();
        return View();
    }

    public async Task<IActionResult> PartnerPerformance()
    {
        var partners = await _context.ChannelPartners
            .AsNoTracking()
            .Include(p => p.Clients)
            .Include(p => p.Proposals)
                .ThenInclude(pr => pr.Service)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.CompanyName)
            .ToListAsync();

        var rows = partners.Select(p =>
        {
            var proposals = p.Proposals ?? new List<PartnerProposal>();
            var amount = proposals.Sum(pr => pr.Amount);
            var commission = proposals.Sum(pr =>
                pr.Service != null ? Math.Round(pr.Amount * pr.Service.Commission / 100m, 2) : 0m);

            return new PartnerPerformanceRow
            {
                PartnerId = p.Id,
                CompanyName = p.CompanyName,
                OwnerName = p.OwnerName,
                IsActive = p.IsActive,
                ClientCount = p.Clients?.Count ?? 0,
                ProposalCount = proposals.Count,
                ProposalAmount = amount,
                EstimatedCommission = commission,
                LastProposalAt = proposals.Count == 0 ? null : proposals.Max(pr => pr.CreatedAt)
            };
        }).OrderByDescending(r => r.ProposalAmount).ThenByDescending(r => r.ClientCount).ToList();

        var vm = new PartnerPerformanceViewModel
        {
            TotalPartners = rows.Count,
            ActivePartners = rows.Count(r => r.IsActive),
            TotalClients = rows.Sum(r => r.ClientCount),
            TotalProposals = rows.Sum(r => r.ProposalCount),
            TotalProposalAmount = rows.Sum(r => r.ProposalAmount),
            TotalEstimatedCommission = rows.Sum(r => r.EstimatedCommission),
            Rows = rows
        };

        return View(vm);
    }

    public async Task<IActionResult> ChannelPartners()
    {
        var partners = await _context.ChannelPartners
            .Include(p => p.Clients)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(partners);
    }

    public IActionResult AddChannelPartner() => View(new ChannelPartner());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> AddChannelPartner(ChannelPartner model, IFormFile? logoFile, string password)
    {
        ModelState.Remove(nameof(ChannelPartner.PasswordHash));
        ModelState.Remove(nameof(ChannelPartner.Clients));
        ModelState.Remove(nameof(ChannelPartner.Proposals));

        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            ModelState.AddModelError("", "Password must be at least 4 characters.");

        if (await _context.ChannelPartners.AnyAsync(p => p.Email == model.Email.Trim()))
            ModelState.AddModelError(nameof(model.Email), "A partner with this email already exists.");

        if (!ModelState.IsValid)
            return View(model);

        model.Email = model.Email.Trim().ToLowerInvariant();
        model.PasswordHash = password.Trim();
        model.IsActive = true;
        model.CreatedAt = DateTime.Now;

        if (logoFile is { Length: > 0 })
        {
            try
            {
                model.LogoPath = await SavePartnerLogoAsync(logoFile);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        _context.ChannelPartners.Add(model);
        await _context.SaveChangesAsync();

        var company = await _companyProfile.GetAsync();
        var loginUrl = Url.Action("Login", "Partner", null, Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/Partner/Login";
        var partnersUrl = Url.Action("Partners", "Home", null, Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/Home/Partners";
        var subject = $"Welcome aboard — you're now a Softflip Channel Partner";
        var html = BuildPartnerWelcomeEmail(model, password.Trim(), loginUrl, partnersUrl, company);
        var mailed = await _emailService.SendEmailAsync(model.Email, subject, html);

        TempData["SuccessMessage"] = mailed
            ? $"Channel partner \"{model.CompanyName}\" created, listed on the website, and welcome email sent to {model.Email}."
            : $"Channel partner \"{model.CompanyName}\" created and listed. Welcome email could not be sent — check SMTP in Settings.";
        return RedirectToAction(nameof(ChannelPartners));
    }

    public async Task<IActionResult> ChannelPartnerDetails(int id)
    {
        var partner = await _context.ChannelPartners
            .Include(p => p.Clients.OrderByDescending(c => c.CreatedAt))
            .Include(p => p.Proposals.OrderByDescending(pr => pr.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (partner == null) return NotFound();
        return View(partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleChannelPartner(int id)
    {
        var partner = await _context.ChannelPartners.FindAsync(id);
        if (partner == null) return NotFound();
        partner.IsActive = !partner.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = partner.IsActive
            ? $"{partner.CompanyName} is active and listed on the website."
            : $"{partner.CompanyName} is deactivated and hidden from the website.";
        return RedirectToAction(nameof(ChannelPartnerDetails), new { id });
    }

    public async Task<IActionResult> EditChannelPartner(int id)
    {
        var partner = await _context.ChannelPartners.FindAsync(id);
        if (partner == null) return NotFound();
        return View(partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> EditChannelPartner(int id, ChannelPartner model, IFormFile? logoFile, string? password, bool removeLogo = false)
    {
        var partner = await _context.ChannelPartners.FindAsync(id);
        if (partner == null) return NotFound();

        ModelState.Remove(nameof(ChannelPartner.PasswordHash));
        ModelState.Remove(nameof(ChannelPartner.Clients));
        ModelState.Remove(nameof(ChannelPartner.Proposals));
        ModelState.Remove(nameof(ChannelPartner.LogoPath));

        var email = (model.Email ?? "").Trim().ToLowerInvariant();
        if (await _context.ChannelPartners.AnyAsync(p => p.Email == email && p.Id != id))
            ModelState.AddModelError(nameof(model.Email), "A partner with this email already exists.");

        if (!string.IsNullOrWhiteSpace(password) && password.Trim().Length < 4)
            ModelState.AddModelError("", "Password must be at least 4 characters.");

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.LogoPath = partner.LogoPath;
            model.IsActive = partner.IsActive;
            model.CreatedAt = partner.CreatedAt;
            return View(model);
        }

        partner.CompanyName = model.CompanyName.Trim();
        partner.OwnerName = model.OwnerName.Trim();
        partner.Gstin = string.IsNullOrWhiteSpace(model.Gstin) ? null : model.Gstin.Trim();
        partner.Mobile = model.Mobile.Trim();
        partner.Email = email;
        partner.Address = model.Address.Trim();
        partner.City = model.City.Trim();
        partner.State = model.State.Trim();
        partner.Pincode = model.Pincode.Trim();
        partner.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();

        if (!string.IsNullOrWhiteSpace(password))
            partner.PasswordHash = password.Trim();

        if (removeLogo && !string.IsNullOrWhiteSpace(partner.LogoPath))
        {
            TryDeletePartnerUpload(partner.LogoPath);
            partner.LogoPath = null;
        }

        if (logoFile is { Length: > 0 })
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(partner.LogoPath))
                    TryDeletePartnerUpload(partner.LogoPath);
                partner.LogoPath = await SavePartnerLogoAsync(logoFile);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.Id = id;
                model.LogoPath = partner.LogoPath;
                return View(model);
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Partner \"{partner.CompanyName}\" updated.";
        return RedirectToAction(nameof(ChannelPartnerDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChannelPartner(int id)
    {
        var partner = await _context.ChannelPartners
            .Include(p => p.Proposals)
            .Include(p => p.Clients)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (partner == null) return NotFound();

        var name = partner.CompanyName;
        var logo = partner.LogoPath;

        if (partner.Proposals.Any())
            _context.PartnerProposals.RemoveRange(partner.Proposals);

        _context.ChannelPartners.Remove(partner);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(logo))
            TryDeletePartnerUpload(logo);

        TempData["SuccessMessage"] = $"Partner \"{name}\" deleted.";
        return RedirectToAction(nameof(ChannelPartners));
    }

    private void TryDeletePartnerUpload(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath)) return;
        try
        {
            var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (!relative.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return;
            var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
            var root = Path.GetFullPath(_env.WebRootPath);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }
        catch { /* ignore file cleanup errors */ }
    }

    public async Task<IActionResult> PartnerClients()
    {
        var clients = await _context.PartnerClients
            .Include(c => c.ChannelPartner)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(clients);
    }

    public async Task<IActionResult> PartnerClientDetails(int id)
    {
        var client = await _context.PartnerClients
            .Include(c => c.ChannelPartner)
            .Include(c => c.Proposals.OrderByDescending(p => p.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return NotFound();
        return View(client);
    }

    // --- Service catalog ---

    public async Task<IActionResult> Services()
    {
        var services = await _context.ServiceCatalogs
            .Include(s => s.Panels)
                .ThenInclude(p => p.Modules)
                    .ThenInclude(m => m.SubModules)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return View(services);
    }

    public IActionResult AddService()
    {
        ViewBag.ServiceNameOptions = EnquiryRequirements.All;
        return View(new ServiceCatalog());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> AddService(ServiceCatalog model)
    {
        ViewBag.ServiceNameOptions = EnquiryRequirements.All;
        ModelState.Remove(nameof(ServiceCatalog.Panels));
        if (!EnquiryRequirements.IsValid(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Please select a valid service from the list.");
        if (!ModelState.IsValid)
            return View(model);

        model.Name = model.Name.Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.CreatedAt = DateTime.Now;
        model.IsActive = true;
        ProposalModuleSelectionHelper.EnsureDefaultPanels(model);

        _context.ServiceCatalogs.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Service \"{model.Name}\" created with Admin, User & Franchise panels. Upload features per panel below.";
        return RedirectToAction(nameof(ServiceDetails), new { id = model.Id });
    }

    public async Task<IActionResult> ServiceDetails(int id)
    {
        var service = await _context.ServiceCatalogs
            .Include(s => s.Panels.OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Modules.OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.SubModules.OrderBy(sm => sm.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return NotFound();
        return View(service);
    }

    public async Task<IActionResult> EditService(int id)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);
        if (service == null) return NotFound();
        ViewBag.ServiceNameOptions = EnquiryRequirements.All;
        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(int id, ServiceCatalog model)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);
        if (service == null) return NotFound();

        ViewBag.ServiceNameOptions = EnquiryRequirements.All;
        ModelState.Remove(nameof(ServiceCatalog.Panels));
        if (!EnquiryRequirements.IsValid(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Please select a valid service from the list.");
        if (!ModelState.IsValid)
            return View(model);

        service.Name = model.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        service.Budget = model.Budget;
        service.Commission = model.Commission;
        service.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Service updated.";
        return RedirectToAction(nameof(ServiceDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddServicePanel(int serviceId, string panelName)
    {
        var service = await _context.ServiceCatalogs
            .Include(s => s.Panels)
            .FirstOrDefaultAsync(s => s.Id == serviceId);
        if (service == null) return NotFound();

        panelName = (panelName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(panelName))
        {
            TempData["ErrorMessage"] = "Panel name is required.";
            return RedirectToAction(nameof(ServiceDetails), new { id = serviceId });
        }

        if (service.Panels.Any(p => p.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] = $"Panel \"{panelName}\" already exists.";
            return RedirectToAction(nameof(ServiceDetails), new { id = serviceId });
        }

        service.Panels.Add(new ServicePanel
        {
            Name = panelName,
            SortOrder = service.Panels.Count == 0 ? 0 : service.Panels.Max(p => p.SortOrder) + 1
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Panel \"{panelName}\" added.";
        return RedirectToAction(nameof(ServiceDetails), new { id = serviceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteServicePanel(int panelId)
    {
        var panel = await _context.ServicePanels
            .Include(p => p.Modules)
                .ThenInclude(m => m.SubModules)
            .FirstOrDefaultAsync(p => p.Id == panelId);
        if (panel == null) return NotFound();

        var serviceId = panel.ServiceCatalogId;
        var name = panel.Name;
        _context.ServicePanels.Remove(panel);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Panel \"{name}\" deleted.";
        return RedirectToAction(nameof(ServiceDetails), new { id = serviceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadPanelModules(int panelId, IFormFile modulesFile)
    {
        var panel = await _context.ServicePanels
            .Include(p => p.Modules)
                .ThenInclude(m => m.SubModules)
            .FirstOrDefaultAsync(p => p.Id == panelId);
        if (panel == null) return NotFound();

        if (modulesFile == null || modulesFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Choose an Excel file (.xlsx).";
            return RedirectToAction(nameof(ServiceDetails), new { id = panel.ServiceCatalogId });
        }

        try
        {
            await using var stream = modulesFile.OpenReadStream();
            var rows = ServiceModuleExcelParser.Parse(stream);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel has no feature rows. Use columns: Feature | Sub Feature.";
                return RedirectToAction(nameof(ServiceDetails), new { id = panel.ServiceCatalogId });
            }

            _context.ServiceModules.RemoveRange(panel.Modules);
            await _context.SaveChangesAsync();

            panel = await _context.ServicePanels
                .Include(p => p.Modules)
                .FirstAsync(p => p.Id == panelId);

            ServiceModuleExcelParser.ApplyToPanel(panel, rows);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"\"{panel.Name}\" updated — {panel.Modules.Count} feature(s) imported.";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Could not read Excel file. Upload a valid .xlsx.";
        }

        return RedirectToAction(nameof(ServiceDetails), new { id = panel.ServiceCatalogId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleService(int id)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);
        if (service == null) return NotFound();
        service.IsActive = !service.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = service.IsActive ? "Service activated." : "Service deactivated.";
        return RedirectToAction(nameof(ServiceDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.ServiceCatalogs
            .Include(s => s.Panels)
                .ThenInclude(p => p.Modules)
                    .ThenInclude(m => m.SubModules)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return NotFound();

        var name = service.Name;
        _context.ServiceCatalogs.Remove(service);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Service \"{name}\" deleted.";
        return RedirectToAction(nameof(Services));
    }

    public IActionResult DownloadServiceModulesTemplate()
    {
        var bytes = ServiceModuleExcelParser.CreateSampleTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "panel-features-template.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> GetServiceModules(int id)
    {
        var service = await _context.ServiceCatalogs
            .AsNoTracking()
            .Include(s => s.Panels.OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Modules.OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.SubModules.OrderBy(sm => sm.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (service == null) return NotFound();

        return Json(new
        {
            id = service.Id,
            name = service.Name,
            panels = service.Panels.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                modules = p.Modules.Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    subModules = m.SubModules.Select(sm => new { id = sm.Id, name = sm.Name })
                })
            })
        });
    }

    private async Task<string> SavePartnerLogoAsync(IFormFile file)
    {
        if (file.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("Logo must be under 2 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new InvalidOperationException("Logo must be PNG, JPG, or WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners");
        Directory.CreateDirectory(dir);
        var name = $"partner-{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var full = Path.Combine(dir, name);
        await using var stream = System.IO.File.Create(full);
        await file.CopyToAsync(stream);
        return $"/uploads/partners/{name}";
    }

    private static string BuildPartnerWelcomeEmail(
        ChannelPartner partner,
        string password,
        string loginUrl,
        string partnersUrl,
        CompanyProfile company)
    {
        var brand = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(company.CompanyName) ? "Softflip Solutions" : company.CompanyName);
        var owner = System.Net.WebUtility.HtmlEncode(partner.OwnerName);
        var companyName = System.Net.WebUtility.HtmlEncode(partner.CompanyName);
        var email = System.Net.WebUtility.HtmlEncode(partner.Email);
        var location = System.Net.WebUtility.HtmlEncode(partner.LocationLabel);
        var safeLogin = System.Net.WebUtility.HtmlEncode(loginUrl);
        var safePartners = System.Net.WebUtility.HtmlEncode(partnersUrl);
        var safePassword = System.Net.WebUtility.HtmlEncode(password);
        var contactPhone = System.Net.WebUtility.HtmlEncode(company.ContactPhone ?? "");
        var contactEmail = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(company.ContactEmail) ? "" : company.ContactEmail);

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#eef3f8;font-family:Segoe UI,Arial,sans-serif;color:#152238'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#eef3f8;padding:28px 12px'>
    <tr><td align='center'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 28px rgba(16,24,40,.08)'>
        <tr>
          <td style='background:linear-gradient(135deg,#12263a 0%,#0b3d5c 55%,#00aeef 140%);padding:28px 28px 24px;color:#fff'>
            <div style='font-size:12px;letter-spacing:.08em;text-transform:uppercase;opacity:.8;margin-bottom:8px'>Channel Partner</div>
            <h1 style='margin:0;font-size:24px;line-height:1.3;font-weight:700'>Welcome to the Softflip family</h1>
            <p style='margin:10px 0 0;opacity:.9;font-size:14px'>{brand} · Authorized Channel Partner</p>
          </td>
        </tr>
        <tr>
          <td style='padding:28px'>
            <p style='margin:0 0 14px;font-size:15px'>Hi <strong>{owner}</strong>,</p>
            <p style='margin:0 0 16px;font-size:15px;line-height:1.55;color:#374151'>
              Thank you for joining us as a Channel Partner. <strong>{companyName}</strong> is now part of our partner network
              and is listed on our website.
            </p>

            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f4fafd;border:1px solid #d7eef8;border-radius:12px;margin:0 0 20px'>
              <tr>
                <td style='padding:16px 18px'>
                  <div style='font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:.04em;margin-bottom:8px'>Your listing</div>
                  <div style='font-size:16px;font-weight:700;color:#152238'>{companyName}</div>
                  <div style='font-size:13px;color:#6b7280;margin-top:4px'>{location}</div>
                  <a href='{safePartners}' style='display:inline-block;margin-top:12px;color:#00aeef;font-size:13px;font-weight:600;text-decoration:none'>View on website →</a>
                </td>
              </tr>
            </table>

            <p style='margin:0 0 10px;font-size:15px;font-weight:600'>Your partner panel login</p>
            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f8f9fb;border:1px solid #e8eaef;border-radius:12px;margin:0 0 18px'>
              <tr>
                <td style='padding:16px 18px;font-size:14px;line-height:1.7'>
                  <div><span style='color:#6b7280'>Login URL:</span> <a href='{safeLogin}' style='color:#00aeef;text-decoration:none'>{safeLogin}</a></div>
                  <div><span style='color:#6b7280'>Email:</span> <strong>{email}</strong></div>
                  <div><span style='color:#6b7280'>Password:</span> <strong>{safePassword}</strong></div>
                </td>
              </tr>
            </table>

            <a href='{safeLogin}' style='display:inline-block;background:#00aeef;color:#fff;text-decoration:none;font-weight:600;font-size:14px;padding:12px 22px;border-radius:999px'>Open Partner Panel</a>

            <p style='margin:22px 0 0;font-size:14px;line-height:1.55;color:#4b5563'>
              Inside the panel you can add interested clients and create quotations / proposals with your own company branding.
            </p>

            <p style='margin:22px 0 0;font-size:14px;color:#374151'>
              Warm regards,<br/>
              <strong>{brand}</strong> Team
              {(string.IsNullOrWhiteSpace(contactPhone) && string.IsNullOrWhiteSpace(contactEmail) ? "" : $"<br/><span style='color:#6b7280;font-size:13px'>{contactPhone}{(string.IsNullOrWhiteSpace(contactPhone) || string.IsNullOrWhiteSpace(contactEmail) ? "" : " · ")}{contactEmail}</span>")}
            </p>
          </td>
        </tr>
        <tr>
          <td style='background:#f8f9fb;padding:14px 28px;font-size:12px;color:#9ca3af;border-top:1px solid #eef0f4'>
            This email was sent because you were added as a Softflip Channel Partner.
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
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
        await PopulateDealPanelAsync(LeadPipeline.LeadEnquiry, id, enquiry.Name, enquiry.Requirement, null);
        await PopulateDocumentsPanelAsync(LeadPipeline.LeadEnquiry, id, enquiry.Status);
        return View(enquiry);
    }

    [HttpPost]
    public async Task<IActionResult> AddEnquiryNote(int id, string noteText)
    {
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var enquiry = await _context.Enquiries.FindAsync(id);
            bool isPost = LeadPipeline.IsActiveDeal(enquiry?.Status);
            _context.EnquiryNotes.Add(new EnquiryNote { EnquiryId = id, NoteText = noteText, IsPostConfirmation = isPost });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(EnquiryDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEnquiryStatus(int id, string status)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry != null && (status == LeadPipeline.Confirmed || status == LeadPipeline.Rejected))
        {
            enquiry.Status = status;
            await _context.SaveChangesAsync();
        }
        return status == LeadPipeline.Confirmed
            ? RedirectToAction(nameof(EnquiryDetails), new { id })
            : RedirectToAction(nameof(RejectedClients));
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
        var active = LeadPipeline.ActiveDealStages;
        ViewBag.Enquiries = await _context.Enquiries.Where(e => active.Contains(e.Status)).OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.DemoRequests = await _context.DemoRequests.Where(e => active.Contains(e.Status)).OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.ClientLeads = await _context.ClientLeads.Where(e => active.Contains(e.Status)).OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View();
    }

    public async Task<IActionResult> RejectedClients()
    {
        ViewBag.Enquiries = await _context.Enquiries.Where(e => e.Status == "Rejected").OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.DemoRequests = await _context.DemoRequests.Where(e => e.Status == "Rejected").OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.ClientLeads = await _context.ClientLeads.Where(e => e.Status == "Rejected").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View();
    }

    // --- External Client Leads (WhatsApp, Just Dial, IndiaMART, Other) ---

    public async Task<IActionResult> ClientLeads()
    {
        var leads = await _context.ClientLeads
            .Where(e => e.Status == "Pending" || e.Status == "")
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(leads);
    }

    public async Task<IActionResult> AddClientLead()
    {
        await PopulateLeadSourcesAsync();
        return View(new ClientLead());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClientLead(ClientLead model, string? sourceChoice, string? customSource)
    {
        model.Source = ResolveLeadSource(sourceChoice, customSource);

        if (string.IsNullOrWhiteSpace(model.Source))
        {
            ModelState.AddModelError(nameof(model.Source), "Please select or enter a source.");
        }

        // Email is optional — clear empty string validation noise
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            model.Email = null;
            ModelState.Remove(nameof(model.Email));
        }

        if (!ModelState.IsValid)
        {
            await PopulateLeadSourcesAsync(sourceChoice);
            return View(model);
        }

        model.Status = "Pending";
        model.CreatedAt = DateTime.Now;
        _context.ClientLeads.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Client lead added successfully.";
        return RedirectToAction(nameof(ClientLeadDetails), new { id = model.Id });
    }

    public async Task<IActionResult> ClientLeadDetails(int id)
    {
        var lead = await _context.ClientLeads
            .Include(e => e.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(e => e.Id == id);
        if (lead == null) return NotFound();
        await PopulateDealPanelAsync(LeadPipeline.LeadClient, id, lead.Name, lead.Requirement, lead.Budget);
        await PopulateDocumentsPanelAsync(LeadPipeline.LeadClient, id, lead.Status);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClientLeadNote(int id, string noteText)
    {
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var lead = await _context.ClientLeads.FindAsync(id);
            bool isPost = LeadPipeline.IsActiveDeal(lead?.Status);
            _context.ClientLeadNotes.Add(new ClientLeadNote
            {
                ClientLeadId = id,
                NoteText = noteText,
                IsPostConfirmation = isPost
            });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ClientLeadDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClientLeadStatus(int id, string status)
    {
        var lead = await _context.ClientLeads.FindAsync(id);
        if (lead != null && (status == LeadPipeline.Confirmed || status == LeadPipeline.Rejected))
        {
            lead.Status = status;
            await _context.SaveChangesAsync();
        }
        return status == LeadPipeline.Confirmed
            ? RedirectToAction(nameof(ClientLeadDetails), new { id })
            : RedirectToAction(nameof(RejectedClients));
    }

    // --- Proposals & Invoices ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateProposal(
        string leadType,
        int leadId,
        string title,
        string scope,
        decimal amount,
        string? templateKey,
        int validDays = 14,
        int? serviceId = null,
        int[]? moduleIds = null,
        int[]? subModuleIds = null)
    {
        var lead = await GetLeadContactAsync(leadType, leadId);
        if (lead == null) return NotFound();
        if (!LeadPipeline.CanGenerateProposal(lead.Status) && lead.Status != LeadPipeline.Invoiced && lead.Status != LeadPipeline.Paid)
        {
            TempData["ErrorMessage"] = "Confirm the lead before generating a proposal.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(scope) || amount <= 0)
        {
            TempData["ErrorMessage"] = "Title, scope and a valid amount are required.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var company = await _companyProfile.GetAsync();
        var template = ProposalTemplates.Get(templateKey);
        var (resolvedServiceId, modulesJson) = await BuildSelectedModulesAsync(serviceId, moduleIds, subModuleIds);

        var proposal = new Proposal
        {
            LeadType = leadType,
            LeadId = leadId,
            Title = title.Trim(),
            Scope = scope.Trim(),
            Amount = amount,
            TemplateKey = template.Key,
            ServiceCatalogId = resolvedServiceId,
            SelectedModulesJson = modulesJson,
            ValidUntil = DateTime.Now.AddDays(Math.Clamp(validDays, 1, 90)),
            CreatedAt = DateTime.Now
        };
        _context.Proposals.Add(proposal);

        if (lead.Status is LeadPipeline.Confirmed or LeadPipeline.ProposalSent)
            await SetLeadStatusAsync(leadType, leadId, LeadPipeline.ProposalSent);

        await _context.SaveChangesAsync();

        var pdf = _dealPdfService.CreateProposalPdf(proposal, lead.Name, lead.Email, lead.Phone, lead.Requirement, company);
        proposal.FilePath = await SaveProposalPdfAsync(proposal.Id, pdf);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proposal ready — download, email, or share on WhatsApp.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendProposalEmail(int proposalId)
    {
        var proposal = await _context.Proposals.FindAsync(proposalId);
        if (proposal == null) return NotFound();
        var lead = await GetLeadContactAsync(proposal.LeadType, proposal.LeadId);
        if (lead == null) return NotFound();
        if (string.IsNullOrWhiteSpace(lead.Email))
        {
            TempData["ErrorMessage"] = "This lead has no email address.";
            return RedirectToLeadDetails(proposal.LeadType, proposal.LeadId);
        }

        var company = await _companyProfile.GetAsync();
        var pdf = await GetOrCreateProposalPdfAsync(proposal, lead, company);
        var subject = $"{company.CompanyName} — {proposal.Title}";
        var html = $@"
        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:0 auto;color:#152238'>
          <div style='background:#152238;color:#fff;padding:20px 24px;border-radius:12px 12px 0 0'>
            <h2 style='margin:0;font-size:20px'>{System.Net.WebUtility.HtmlEncode(company.CompanyName)}</h2>
            <p style='margin:6px 0 0;opacity:.8;font-size:13px'>{System.Net.WebUtility.HtmlEncode(company.Tagline)}</p>
          </div>
          <div style='border:1px solid #e8eaef;border-top:none;padding:24px;border-radius:0 0 12px 12px'>
            <p>Hi {System.Net.WebUtility.HtmlEncode(lead.Name)},</p>
            <p>Please find attached our proposal: <strong>{System.Net.WebUtility.HtmlEncode(proposal.Title)}</strong>.</p>
            <p style='background:#f8f9fb;padding:12px 14px;border-radius:8px'>
              Amount: <strong>₹ {proposal.Amount:N2}</strong><br/>
              Valid until: <strong>{proposal.ValidUntil:dd MMM yyyy}</strong>
            </p>
            <p>Feel free to reply to this email or WhatsApp us if you have questions.</p>
            <p style='margin-top:24px'>Regards,<br/><strong>{System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(company.SignatoryName) ? company.CompanyName : company.SignatoryName)}</strong><br/>
            <span style='color:#6b7280;font-size:13px'>{System.Net.WebUtility.HtmlEncode(company.SignatoryTitle)}</span></p>
          </div>
        </div>";

        var ok = await _emailService.SendEmailAsync(lead.Email, subject, html, pdf, $"Proposal-{proposal.Id}.pdf");
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? $"Proposal emailed to {lead.Email}."
            : "Email failed. Check SMTP settings under Settings → Email.";
        return RedirectToLeadDetails(proposal.LeadType, proposal.LeadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToInvoice(string leadType, int leadId)
    {
        var lead = await GetLeadContactAsync(leadType, leadId);
        if (lead == null) return NotFound();

        var proposal = await _context.Proposals
            .Include(p => p.Invoice)
            .Where(p => p.LeadType == leadType && p.LeadId == leadId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (proposal == null)
        {
            TempData["ErrorMessage"] = "Generate a proposal first.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        if (proposal.Invoice != null)
        {
            TempData["ErrorMessage"] = "An invoice already exists for the latest proposal.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var invoice = new Invoice
        {
            ProposalId = proposal.Id,
            LeadType = leadType,
            LeadId = leadId,
            InvoiceNumber = await NextInvoiceNumberAsync(),
            Title = proposal.Title,
            Description = proposal.Scope,
            Amount = proposal.Amount,
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        };
        _context.Invoices.Add(invoice);
        await SetLeadStatusAsync(leadType, leadId, LeadPipeline.Invoiced);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordInvoicePayment(int invoiceId, decimal amount, string? note, int? percent)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null) return NotFound();

        if (invoice.Status == "Paid")
        {
            TempData["ErrorMessage"] = "Invoice is already fully paid.";
            return RedirectToLeadDetails(invoice.LeadType, invoice.LeadId);
        }

        if (percent is 25 or 50 or 75 or 100)
        {
            amount = Math.Round(invoice.Amount * percent.Value / 100m, 2);
            if (string.IsNullOrWhiteSpace(note))
                note = $"{percent}% payment";
        }

        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Enter a valid payment amount.";
            return RedirectToLeadDetails(invoice.LeadType, invoice.LeadId);
        }

        var remaining = invoice.Balance;
        if (amount > remaining)
            amount = remaining;

        _context.InvoicePayments.Add(new InvoicePayment
        {
            InvoiceId = invoice.Id,
            Amount = amount,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            PaidAt = DateTime.Now
        });

        invoice.AmountPaid += amount;
        if (invoice.AmountPaid >= invoice.Amount)
        {
            invoice.AmountPaid = invoice.Amount;
            invoice.Status = "Paid";
            invoice.PaidAt = DateTime.Now;
            await SetLeadStatusAsync(invoice.LeadType, invoice.LeadId, LeadPipeline.Paid);
            TempData["SuccessMessage"] = $"₹ {amount:N2} recorded. Invoice fully paid.";
        }
        else
        {
            invoice.Status = "Partial";
            invoice.PaidAt = null;
            TempData["SuccessMessage"] = $"₹ {amount:N2} recorded. Balance due: ₹ {invoice.Balance:N2}.";
        }

        await _context.SaveChangesAsync();
        return RedirectToLeadDetails(invoice.LeadType, invoice.LeadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInvoicePaid(int invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null) return NotFound();

        var remaining = invoice.Balance;
        if (remaining > 0)
        {
            _context.InvoicePayments.Add(new InvoicePayment
            {
                InvoiceId = invoice.Id,
                Amount = remaining,
                Note = "Marked fully paid",
                PaidAt = DateTime.Now
            });
            invoice.AmountPaid = invoice.Amount;
        }

        invoice.Status = "Paid";
        invoice.PaidAt = DateTime.Now;
        await SetLeadStatusAsync(invoice.LeadType, invoice.LeadId, LeadPipeline.Paid);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} marked as paid.";
        return RedirectToLeadDetails(invoice.LeadType, invoice.LeadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadLeadDocument(string leadType, int leadId, IFormFile file, string? title, string? category)
    {
        var lead = await GetLeadContactAsync(leadType, leadId);
        if (!IsKnownLeadType(leadType) || lead == null)
            return NotFound();

        if (!LeadPipeline.IsActiveDeal(lead.Status))
        {
            TempData["ErrorMessage"] = "Confirm the client before uploading documents.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please choose a file to upload.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        const long maxBytes = 15 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            TempData["ErrorMessage"] = "File must be 15 MB or smaller.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".webp", ".gif",
            ".txt", ".csv", ".zip", ".rar", ".dwg", ".dxf"
        };
        if (!allowed.Contains(ext))
        {
            TempData["ErrorMessage"] = "File type not allowed. Use PDF, Office, image, ZIP, or CAD files.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var safeCategory = NormalizeDocCategory(category);
        var displayTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : title.Trim();
        if (displayTitle.Length > 200) displayTitle = displayTitle[..200];

        var dir = Path.Combine(_env.WebRootPath, "uploads", "documents");
        Directory.CreateDirectory(dir);
        var storedName = $"{leadType.ToLowerInvariant()}-{leadId}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, storedName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        _context.LeadDocuments.Add(new LeadDocument
        {
            LeadType = leadType,
            LeadId = leadId,
            Category = safeCategory,
            Title = displayTitle,
            OriginalFileName = Path.GetFileName(file.FileName),
            FilePath = $"/uploads/documents/{storedName}",
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Document \"{displayTitle}\" uploaded.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    public async Task<IActionResult> DownloadLeadDocument(int id)
    {
        var doc = await _context.LeadDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        var physical = MapUploadPath(doc.FilePath);
        if (physical == null || !System.IO.File.Exists(physical))
            return NotFound();

        var contentType = string.IsNullOrWhiteSpace(doc.ContentType)
            ? "application/octet-stream"
            : doc.ContentType;
        return PhysicalFile(physical, contentType, doc.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLeadDocument(int id)
    {
        var doc = await _context.LeadDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        var lead = await GetLeadContactAsync(doc.LeadType, doc.LeadId);
        if (lead == null || !LeadPipeline.IsActiveDeal(lead.Status))
        {
            TempData["ErrorMessage"] = "Documents are only available after the client is confirmed.";
            return lead == null ? NotFound() : RedirectToLeadDetails(doc.LeadType, doc.LeadId);
        }

        var leadType = doc.LeadType;
        var leadId = doc.LeadId;
        var physical = MapUploadPath(doc.FilePath);
        if (physical != null && System.IO.File.Exists(physical))
            System.IO.File.Delete(physical);

        _context.LeadDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Document deleted.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    public async Task<IActionResult> DownloadProposal(int id)
    {
        var proposal = await _context.Proposals.FindAsync(id);
        if (proposal == null) return NotFound();
        var lead = await GetLeadContactAsync(proposal.LeadType, proposal.LeadId);
        if (lead == null) return NotFound();

        var company = await _companyProfile.GetAsync();
        var pdf = await GetOrCreateProposalPdfAsync(proposal, lead, company);
        return File(pdf, "application/pdf", $"Proposal-{proposal.Id}.pdf");
    }

    public async Task<IActionResult> DownloadInvoice(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return NotFound();
        var lead = await GetLeadContactAsync(invoice.LeadType, invoice.LeadId);
        if (lead == null) return NotFound();

        var company = await _companyProfile.GetAsync();
        var pdf = _dealPdfService.CreateInvoicePdf(invoice, lead.Name, lead.Email, lead.Phone, company);
        return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    private async Task PopulateDealPanelAsync(string leadType, int leadId, string name, string requirement, string? suggestedAmount)
    {
        var proposal = await _context.Proposals
            .Include(p => p.Invoice!)
                .ThenInclude(i => i.Payments.OrderByDescending(pay => pay.PaidAt))
            .Where(p => p.LeadType == leadType && p.LeadId == leadId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var invoice = proposal?.Invoice
            ?? await _context.Invoices
                .Include(i => i.Payments.OrderByDescending(pay => pay.PaidAt))
                .Where(i => i.LeadType == leadType && i.LeadId == leadId)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync();

        var contact = await GetLeadContactAsync(leadType, leadId);
        string? status = contact?.Status ?? LeadPipeline.Pending;

        var services = await _context.ServiceCatalogs
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Panels.OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Modules.OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.SubModules.OrderBy(sm => sm.SortOrder))
            .OrderBy(s => s.Name)
            .ToListAsync();

        var publicUrl = "";
        if (proposal != null && !string.IsNullOrWhiteSpace(proposal.FilePath))
        {
            publicUrl = $"{Request.Scheme}://{Request.Host}{proposal.FilePath}";
        }
        else if (proposal != null)
        {
            publicUrl = Url.Action(nameof(DownloadProposal), "Admin", new { id = proposal.Id }, Request.Scheme) ?? "";
        }

        ViewBag.DealPanel = new LeadDealPanelViewModel
        {
            LeadType = leadType,
            LeadId = leadId,
            Status = status ?? LeadPipeline.Pending,
            ClientName = name,
            Requirement = requirement,
            SuggestedAmount = suggestedAmount,
            ClientEmail = contact?.Email,
            ClientPhone = contact?.Phone,
            LatestProposal = proposal,
            LatestInvoice = invoice,
            ProposalPublicUrl = publicUrl,
            Services = services
        };
    }

    private async Task<(int? ServiceId, string? ModulesJson)> BuildSelectedModulesAsync(
        int? serviceId, int[]? moduleIds, int[]? subModuleIds) =>
        await ProposalModuleSelectionHelper.BuildSelectionAsync(_context, serviceId, moduleIds, subModuleIds);

    private async Task PopulateDocumentsPanelAsync(string leadType, int leadId, string? status)
    {
        // Documents only after client is confirmed (active deal pipeline).
        if (!LeadPipeline.IsActiveDeal(status))
        {
            ViewBag.DocumentsPanel = null;
            return;
        }

        var docs = await _context.LeadDocuments
            .Where(d => d.LeadType == leadType && d.LeadId == leadId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        ViewBag.DocumentsPanel = new LeadDocumentsPanelViewModel
        {
            LeadType = leadType,
            LeadId = leadId,
            CanEdit = status != LeadPipeline.Rejected,
            Documents = docs
        };
    }

    private static bool IsKnownLeadType(string? leadType) =>
        leadType is LeadPipeline.LeadEnquiry or LeadPipeline.LeadClient or LeadPipeline.LeadDemo;

    private static string NormalizeDocCategory(string? category)
    {
        var value = (category ?? "").Trim();
        return value switch
        {
            "Plan" or "Design" or "Contract" or "Project" or "Other" => value,
            _ => "Project"
        };
    }

    private string? MapUploadPath(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath)) return null;
        var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (!relative.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        var root = Path.GetFullPath(_env.WebRootPath);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    private async Task<string> SaveProposalPdfAsync(int proposalId, byte[] pdf)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "proposals");
        Directory.CreateDirectory(dir);
        var fileName = $"proposal-{proposalId}-{Guid.NewGuid():N}.pdf";
        var fullPath = Path.Combine(dir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, pdf);
        return $"/uploads/proposals/{fileName}";
    }

    private async Task<byte[]> GetOrCreateProposalPdfAsync(Proposal proposal, LeadContact lead, CompanyProfile company)
    {
        // Always regenerate so latest company branding / bank / UPI appear on PDF.
        var pdf = _dealPdfService.CreateProposalPdf(proposal, lead.Name, lead.Email, lead.Phone, lead.Requirement, company);
        proposal.FilePath = await SaveProposalPdfAsync(proposal.Id, pdf);
        await _context.SaveChangesAsync();
        return pdf;
    }

    private async Task<string> NextInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.Now:yyyyMM}-";
        var last = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        var seq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;

        return $"{prefix}{seq:D4}";
    }

    private async Task<LeadContact?> GetLeadContactAsync(string leadType, int leadId)
    {
        if (leadType == LeadPipeline.LeadEnquiry)
        {
            var e = await _context.Enquiries.FindAsync(leadId);
            return e == null ? null : new LeadContact
            {
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                Requirement = e.Requirement,
                Status = e.Status
            };
        }
        if (leadType == LeadPipeline.LeadClient)
        {
            var c = await _context.ClientLeads.FindAsync(leadId);
            return c == null ? null : new LeadContact
            {
                Name = c.Name,
                Email = c.Email,
                Phone = c.Mobile,
                Requirement = c.Requirement,
                Status = c.Status
            };
        }
        if (leadType == LeadPipeline.LeadDemo)
        {
            var d = await _context.DemoRequests.FindAsync(leadId);
            return d == null ? null : new LeadContact
            {
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Requirement = d.Requirement,
                Status = d.Status
            };
        }
        return null;
    }

    private async Task SetLeadStatusAsync(string leadType, int leadId, string status)
    {
        if (leadType == LeadPipeline.LeadEnquiry)
        {
            var e = await _context.Enquiries.FindAsync(leadId);
            if (e != null) e.Status = status;
        }
        else if (leadType == LeadPipeline.LeadClient)
        {
            var c = await _context.ClientLeads.FindAsync(leadId);
            if (c != null) c.Status = status;
        }
        else if (leadType == LeadPipeline.LeadDemo)
        {
            var d = await _context.DemoRequests.FindAsync(leadId);
            if (d != null) d.Status = status;
        }
    }

    private IActionResult RedirectToLeadDetails(string leadType, int leadId) => leadType switch
    {
        LeadPipeline.LeadClient => RedirectToAction(nameof(ClientLeadDetails), new { id = leadId }),
        LeadPipeline.LeadDemo => RedirectToAction(nameof(DemoRequestDetails), new { id = leadId }),
        _ => RedirectToAction(nameof(EnquiryDetails), new { id = leadId })
    };

    private async Task PopulateLeadSourcesAsync(string? selected = null)
    {
        var usedSources = await _context.ClientLeads
            .Select(c => c.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var sources = new List<string>(DefaultLeadSources);
        foreach (var source in usedSources)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                !sources.Exists(s => s.Equals(source, StringComparison.OrdinalIgnoreCase)))
            {
                sources.Add(source);
            }
        }

        ViewBag.LeadSources = sources;
        ViewBag.SelectedSource = selected;
    }

    private static string ResolveLeadSource(string? sourceChoice, string? customSource)
    {
        if (string.Equals(sourceChoice, "Other", StringComparison.OrdinalIgnoreCase))
        {
            return customSource?.Trim() ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(sourceChoice))
        {
            return sourceChoice.Trim();
        }

        return customSource?.Trim() ?? string.Empty;
    }

    // --- Settings & SMTP Management ---

    public async Task<IActionResult> Settings()
    {
        var settingsList = await _context.AdminSettings.ToListAsync();
        var settingsDict = settingsList.ToDictionary(s => s.Key, s => s.Value);

        ViewBag.SmtpHost = settingsDict.ContainsKey("SmtpHost") ? settingsDict["SmtpHost"] : "";
        ViewBag.SmtpPort = settingsDict.ContainsKey("SmtpPort") ? settingsDict["SmtpPort"] : "587";
        ViewBag.SmtpEmail = settingsDict.ContainsKey("SmtpEmail") ? settingsDict["SmtpEmail"] : "";
        ViewBag.SmtpPassword = settingsDict.ContainsKey("SmtpPassword") ? settingsDict["SmtpPassword"] : "";
        ViewBag.SmtpEnableSsl = settingsDict.ContainsKey("SmtpEnableSsl") ? bool.Parse(settingsDict["SmtpEnableSsl"]) : true;

        ViewBag.Company = await _companyProfile.GetAsync();
        ViewBag.ActiveSettingsTab = TempData["SettingsTab"] as string ?? "company";

        var adminUser = await _context.AdminUsers.FirstOrDefaultAsync();
        ViewBag.AdminUsername = adminUser?.Username ?? "admin";

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCompany(
        string CompanyName, string Tagline, string Address, string Gstin, string Website,
        string BankName, string BankAccountName, string BankAccountNumber, string BankIfsc, string BankBranch,
        string UpiId, string UpiName,
        IFormFile? LogoFile, bool RemoveLogo = false)
    {
        TempData["SettingsTab"] = "company";
        try
        {
            var profile = await _companyProfile.GetAsync();
            profile.CompanyName = CompanyName;
            profile.Tagline = Tagline;
            profile.Address = Address;
            profile.Gstin = Gstin;
            profile.Website = Website;
            profile.BankName = BankName;
            profile.BankAccountName = BankAccountName;
            profile.BankAccountNumber = BankAccountNumber;
            profile.BankIfsc = BankIfsc;
            profile.BankBranch = BankBranch;
            profile.UpiId = UpiId;
            profile.UpiName = UpiName;

            if (RemoveLogo) profile.LogoPath = "";
            if (LogoFile != null && LogoFile.Length > 0)
                profile.LogoPath = await _companyProfile.SaveUploadAsync(LogoFile, "branding", "logo") ?? profile.LogoPath;

            await _companyProfile.SaveCompanyAsync(profile);
            TempData["SuccessMessage"] = "Company & payment details saved.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateContacts(string ContactPerson, string ContactPhone, string ContactWhatsApp, string ContactEmail)
    {
        TempData["SettingsTab"] = "contacts";
        var profile = await _companyProfile.GetAsync();
        profile.ContactPerson = ContactPerson;
        profile.ContactPhone = ContactPhone;
        profile.ContactWhatsApp = ContactWhatsApp;
        profile.ContactEmail = ContactEmail;
        await _companyProfile.SaveContactsAsync(profile);
        TempData["SuccessMessage"] = "Contact details saved.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSignature(
        string SignatoryName, string SignatoryTitle,
        IFormFile? SignatureFile, bool RemoveSignature = false)
    {
        TempData["SettingsTab"] = "signature";
        try
        {
            var profile = await _companyProfile.GetAsync();
            profile.SignatoryName = SignatoryName;
            profile.SignatoryTitle = SignatoryTitle;
            if (RemoveSignature) profile.SignaturePath = "";
            if (SignatureFile != null && SignatureFile.Length > 0)
                profile.SignaturePath = await _companyProfile.SaveUploadAsync(SignatureFile, "branding", "sign") ?? profile.SignaturePath;

            await _companyProfile.SaveSignatureAsync(profile);
            TempData["SuccessMessage"] = "Signature settings saved.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSecurity(string currentPassword, string newPassword, string confirmPassword)
    {
        TempData["SettingsTab"] = "security";
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
        TempData["SettingsTab"] = "smtp";
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
