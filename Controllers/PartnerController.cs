using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "PartnerCookie")]
public class PartnerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDealPdfService _dealPdfService;
    private readonly IEmailService _emailService;
    private readonly IPartnerVisitingCardService _visitingCardService;
    private readonly IWebHostEnvironment _env;

    public PartnerController(
        ApplicationDbContext context,
        IDealPdfService dealPdfService,
        IEmailService emailService,
        IPartnerVisitingCardService visitingCardService,
        IWebHostEnvironment env)
    {
        _context = context;
        _dealPdfService = dealPdfService;
        _emailService = emailService;
        _visitingCardService = visitingCardService;
        _env = env;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == "PartnerId"))
            return RedirectToAction(nameof(Index));
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var partner = await _context.ChannelPartners
            .FirstOrDefaultAsync(p => p.Email == email && p.IsActive);

        if (partner == null)
        {
            ViewBag.Error = "Invalid email or password, or account is inactive.";
            return View();
        }

        var hash = partner.PasswordHash;
        if (!PasswordHelper.VerifyAndUpgrade(password, ref hash, out var upgraded))
        {
            ViewBag.Error = "Invalid email or password, or account is inactive.";
            return View();
        }

        if (upgraded)
        {
            partner.PasswordHash = hash!;
            await _context.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, partner.CompanyName),
            new(ClaimTypes.Email, partner.Email),
            new("PartnerId", partner.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "PartnerCookie");
        await HttpContext.SignInAsync("PartnerCookie", new ClaimsPrincipal(identity));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("PartnerCookie");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Index()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        ViewBag.ClientCount = await _context.PartnerClients.CountAsync(c => c.ChannelPartnerId == partner.Id);
        ViewBag.ProposalCount = await _context.PartnerProposals.CountAsync(p => p.ChannelPartnerId == partner.Id);
        ViewBag.RecentClients = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();
        return View();
    }

    public async Task<IActionResult> Clients()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var clients = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .Include(c => c.Proposals)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(clients);
    }

    public async Task<IActionResult> AddClient()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;
        return View(new PartnerClient());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClient(PartnerClient model)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        ModelState.Remove(nameof(PartnerClient.ChannelPartner));
        ModelState.Remove(nameof(PartnerClient.Proposals));
        ModelState.Remove(nameof(PartnerClient.ChannelPartnerId));

        if (!ModelState.IsValid)
            return View(model);

        model.ChannelPartnerId = partner.Id;
        model.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(model.WhatsApp))
            model.WhatsApp = model.Mobile;

        _context.PartnerClients.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Client \"{model.Name}\" added.";
        return RedirectToAction(nameof(ClientDetails), new { id = model.Id });
    }

    public async Task<IActionResult> ClientDetails(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var client = await _context.PartnerClients
            .Include(c => c.Proposals.OrderByDescending(p => p.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.ChannelPartnerId == partner.Id);
        if (client == null) return NotFound();

        ViewBag.Partner = partner;
        ViewBag.Services = await ProposalModuleSelectionHelper.GetActiveServicesAsync(_context);
        return View(client);
    }

    public async Task<IActionResult> Services()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var services = await _context.ServiceCatalogs
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Panels.OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Modules.OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.SubModules.OrderBy(sm => sm.SortOrder))
            .OrderBy(s => s.Name)
            .ToListAsync();
        return View(services);
    }

    public async Task<IActionResult> VisitingCard()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var cardUrl = await _visitingCardService.EnsureCardImageAsync(partner);
        ViewBag.CardImageUrl = $"{Request.Scheme}://{Request.Host}{cardUrl}";
        ViewBag.CardImagePath = cardUrl;
        return View(partner);
    }

    public async Task<IActionResult> DownloadVisitingCard()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var png = await _visitingCardService.CreateCardImageAsync(partner);
        var safeName = string.Join("_", partner.CompanyName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "visiting-card";
        return File(png, "image/png", $"{safeName}-visiting-card.png");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProposal(
        int clientId,
        string title,
        string scope,
        decimal amount,
        int validDays = 15,
        string? templateKey = "classic",
        int? serviceId = null)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var client = await _context.PartnerClients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.ChannelPartnerId == partner.Id);
        if (client == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(scope))
        {
            TempData["ErrorMessage"] = "Title and scope are required.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        if (!serviceId.HasValue || serviceId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Please select a service.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        var catalogService = await _context.ServiceCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId.Value && s.IsActive);
        if (catalogService == null)
        {
            TempData["ErrorMessage"] = "Selected service is not available.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        // Force admin-set budget; ignore any client-tampered amount. Commission is never stored on proposal.
        amount = catalogService.Budget;
        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Selected service has no valid budget. Contact Softflip admin.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        var (resolvedServiceId, modulesJson) =
            await ProposalModuleSelectionHelper.BuildFullServiceSelectionAsync(_context, serviceId);

        var proposal = new PartnerProposal
        {
            ChannelPartnerId = partner.Id,
            PartnerClientId = client.Id,
            Title = title.Trim(),
            Scope = scope.Trim(),
            Amount = amount,
            TemplateKey = string.IsNullOrWhiteSpace(templateKey) ? "classic" : templateKey.Trim(),
            ServiceCatalogId = resolvedServiceId,
            SelectedModulesJson = modulesJson,
            ValidUntil = DateTime.Now.AddDays(Math.Clamp(validDays, 1, 90)),
            CreatedAt = DateTime.Now
        };

        var company = partner.ToCompanyProfile();
        var pdf = _dealPdfService.CreateProposalPdf(
            ToPdfModel(proposal),
            client.Name,
            client.Email,
            client.WhatsApp ?? client.Mobile,
            client.Requirement,
            company);

        proposal.FilePath = await SavePartnerProposalPdfAsync(partner.Id, pdf);
        _context.PartnerProposals.Add(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proposal created with your company branding.";
        return RedirectToAction(nameof(ClientDetails), new { id = clientId });
    }

    public async Task<IActionResult> DownloadProposal(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .FirstOrDefaultAsync(p => p.Id == id && p.ChannelPartnerId == partner.Id);
        if (proposal == null) return NotFound();

        var pdf = await GetOrCreatePartnerProposalPdfAsync(proposal, partner);
        return File(pdf, "application/pdf", $"Proposal-{proposal.Id}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendProposalEmail(int proposalId)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .FirstOrDefaultAsync(p => p.Id == proposalId && p.ChannelPartnerId == partner.Id);
        if (proposal?.PartnerClient == null) return NotFound();

        var client = proposal.PartnerClient;
        if (string.IsNullOrWhiteSpace(client.Email))
        {
            TempData["ErrorMessage"] = "This client has no email address.";
            return RedirectToAction(nameof(ClientDetails), new { id = client.Id });
        }

        var company = partner.ToCompanyProfile();
        var pdf = await GetOrCreatePartnerProposalPdfAsync(proposal, partner);
        var subject = $"{company.CompanyName} — {proposal.Title}";
        var html = $@"
        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:0 auto;color:#152238'>
          <div style='background:#152238;color:#fff;padding:20px 24px;border-radius:12px 12px 0 0'>
            <h2 style='margin:0;font-size:20px'>{System.Net.WebUtility.HtmlEncode(company.CompanyName)}</h2>
            <p style='margin:6px 0 0;opacity:.8;font-size:13px'>{System.Net.WebUtility.HtmlEncode(company.Tagline)}</p>
          </div>
          <div style='border:1px solid #e8eaef;border-top:none;padding:24px;border-radius:0 0 12px 12px'>
            <p>Hi {System.Net.WebUtility.HtmlEncode(client.Name)},</p>
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

        var ok = await _emailService.SendEmailAsync(client.Email, subject, html, pdf, $"Proposal-{proposal.Id}.pdf");
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? $"Proposal emailed to {client.Email}."
            : "Email failed. Ask Softflip admin to check SMTP settings.";
        return RedirectToAction(nameof(ClientDetails), new { id = client.Id });
    }

    private static Proposal ToPdfModel(PartnerProposal proposal) => new()
    {
        Title = proposal.Title,
        Scope = proposal.Scope,
        Amount = proposal.Amount,
        TemplateKey = proposal.TemplateKey,
        ValidUntil = proposal.ValidUntil,
        CreatedAt = proposal.CreatedAt,
        ServiceCatalogId = proposal.ServiceCatalogId,
        SelectedModulesJson = proposal.SelectedModulesJson
    };

    private async Task<ChannelPartner?> CurrentPartnerAsync()
    {
        var idClaim = User.FindFirst("PartnerId")?.Value;
        if (!int.TryParse(idClaim, out var id)) return null;
        return await _context.ChannelPartners.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }

    private async Task<byte[]> GetOrCreatePartnerProposalPdfAsync(PartnerProposal proposal, ChannelPartner partner)
    {
        var client = proposal.PartnerClient
            ?? await _context.PartnerClients.FindAsync(proposal.PartnerClientId)
            ?? throw new InvalidOperationException("Client not found.");

        var company = partner.ToCompanyProfile();
        var pdf = _dealPdfService.CreateProposalPdf(
            ToPdfModel(proposal),
            client.Name,
            client.Email,
            client.WhatsApp ?? client.Mobile,
            client.Requirement,
            company);

        // Reuse existing public path so WhatsApp/email links stay valid after regenerate.
        if (!string.IsNullOrWhiteSpace(proposal.FilePath)
            && proposal.FilePath.StartsWith("/uploads/partners/proposals/", StringComparison.OrdinalIgnoreCase))
        {
            var physical = Path.Combine(_env.WebRootPath, proposal.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
            await System.IO.File.WriteAllBytesAsync(physical, pdf);
        }
        else
        {
            proposal.FilePath = await SavePartnerProposalPdfAsync(partner.Id, pdf);
            await _context.SaveChangesAsync();
        }

        return pdf;
    }

    private async Task<string> SavePartnerProposalPdfAsync(int partnerId, byte[] pdf)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners", "proposals");
        Directory.CreateDirectory(dir);
        var name = $"pp-{partnerId}-{Guid.NewGuid():N}.pdf";
        var full = Path.Combine(dir, name);
        await System.IO.File.WriteAllBytesAsync(full, pdf);
        return $"/uploads/partners/proposals/{name}";
    }
}
