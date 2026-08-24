using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "PartnerCookie")]
public partial class PartnerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDealPdfService _dealPdfService;
    private readonly IEmailService _emailService;
    private readonly IPartnerVisitingCardService _visitingCardService;
    private readonly IPartnerCertificateService _certificateService;
    private readonly IWebHostEnvironment _env;

    public PartnerController(
        ApplicationDbContext context,
        IDealPdfService dealPdfService,
        IEmailService emailService,
        IPartnerVisitingCardService visitingCardService,
        IPartnerCertificateService certificateService,
        IWebHostEnvironment env)
    {
        _context = context;
        _dealPdfService = dealPdfService;
        _emailService = emailService;
        _visitingCardService = visitingCardService;
        _certificateService = certificateService;
        _env = env;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == "PartnerId"))
        {
            var partnerIdClaim = User.FindFirst("PartnerId")?.Value;
            if (int.TryParse(partnerIdClaim, out var partnerId))
            {
                ViewBag.ActivePartnerMeetings = await GetActiveMeetingsForPartnerAsync(partnerId);
                ViewBag.UnreadPartnerNotifications = await _context.PartnerNotifications
                    .CountAsync(n => n.ChannelPartnerId == partnerId && !n.IsRead);
            }
        }
        await next();
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
        var clientIds = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .Select(c => c.Id)
            .ToListAsync();
        ViewBag.OverdueFollowUps = await _context.FollowUpReminders.CountAsync(f =>
            f.LeadType == LeadPipeline.LeadPartnerClient
            && clientIds.Contains(f.LeadId)
            && !f.IsDone
            && f.DueAt < DateTime.Now);
        ViewBag.RecentClients = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();
        return View();
    }

    public async Task<IActionResult> Meetings()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var meetings = await GetActiveMeetingsForPartnerAsync(partner.Id);
        return View(meetings);
    }

    private async Task<List<PartnerMeeting>> GetActiveMeetingsForPartnerAsync(int partnerId)
    {
        var now = DateTime.Now;
        return await _context.PartnerMeetings
            .AsNoTracking()
            .Where(m => m.IsActive && m.MeetingAt >= now
                && (m.AssignToAllPartners
                    || m.Assignments.Any(a => a.ChannelPartnerId == partnerId)))
            .OrderBy(m => m.MeetingAt)
            .ToListAsync();
    }

    public async Task<IActionResult> FollowUps(string? show)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var clientIds = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .Select(c => c.Id)
            .ToListAsync();
        var nameMap = await _context.PartnerClients
            .AsNoTracking()
            .Where(c => c.ChannelPartnerId == partner.Id)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var showDone = string.Equals(show, "done", StringComparison.OrdinalIgnoreCase);
        var query = _context.FollowUpReminders.AsNoTracking()
            .Where(f => f.LeadType == LeadPipeline.LeadPartnerClient && clientIds.Contains(f.LeadId));
        if (!showDone)
            query = query.Where(f => !f.IsDone);

        var items = await query
            .OrderBy(f => f.IsDone)
            .ThenBy(f => f.DueAt)
            .ToListAsync();

        var rows = items.Select(f => new SoftflipSolutions.ViewModels.FollowUpReminderItem
        {
            Id = f.Id,
            LeadType = f.LeadType,
            LeadId = f.LeadId,
            LeadName = nameMap.TryGetValue(f.LeadId, out var n) ? n : $"Client #{f.LeadId}",
            StepType = f.StepType,
            DueAt = f.DueAt,
            Note = f.Note,
            IsDone = f.IsDone,
            CreatedAt = f.CreatedAt,
            CompletedAt = f.CompletedAt
        }).ToList();

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFollowUp(int leadId, string stepType, DateTime dueAt, string note)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var ownsClient = await _context.PartnerClients
            .AnyAsync(c => c.Id == leadId && c.ChannelPartnerId == partner.Id);
        if (!ownsClient || string.IsNullOrWhiteSpace(note))
            return RedirectToAction(nameof(ClientDetails), new { id = leadId });

        var step = FollowUpSteps.IsKnown(stepType) ? stepType.Trim() : FollowUpSteps.Note;

        _context.FollowUpReminders.Add(new FollowUpReminder
        {
            LeadType = LeadPipeline.LeadPartnerClient,
            LeadId = leadId,
            StepType = step,
            DueAt = dueAt == default ? DateTime.Now.AddDays(1) : dueAt,
            Note = note.Trim(),
            IsDone = false,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{FollowUpSteps.Get(step).Label} follow-up scheduled.";
        return RedirectToAction(nameof(ClientDetails), new { id = leadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFollowUp(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var item = await _context.FollowUpReminders.FindAsync(id);
        if (item == null || item.LeadType != LeadPipeline.LeadPartnerClient)
            return NotFound();

        var ownsClient = await _context.PartnerClients
            .AnyAsync(c => c.Id == item.LeadId && c.ChannelPartnerId == partner.Id);
        if (!ownsClient) return NotFound();

        item.IsDone = true;
        item.CompletedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Follow-up marked done.";

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && referer.Contains("/Partner/FollowUps", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(FollowUps));

        return RedirectToAction(nameof(ClientDetails), new { id = item.LeadId });
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
        model.Stage = PartnerClientStages.New;
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
        await PopulateFollowUpsPanelAsync(client.Id);
        return View(client);
    }

    private async Task PopulateFollowUpsPanelAsync(int clientId)
    {
        var items = await _context.FollowUpReminders
            .AsNoTracking()
            .Where(f => f.LeadType == LeadPipeline.LeadPartnerClient && f.LeadId == clientId)
            .OrderBy(f => f.IsDone)
            .ThenBy(f => f.DueAt)
            .ToListAsync();

        ViewBag.FollowUpsPanel = new SoftflipSolutions.ViewModels.LeadFollowUpsPanelViewModel
        {
            LeadType = LeadPipeline.LeadPartnerClient,
            LeadId = clientId,
            Items = items.Select(f => new SoftflipSolutions.ViewModels.FollowUpReminderItem
            {
                Id = f.Id,
                LeadType = f.LeadType,
                LeadId = f.LeadId,
                StepType = f.StepType,
                DueAt = f.DueAt,
                Note = f.Note,
                IsDone = f.IsDone,
                CreatedAt = f.CreatedAt,
                CompletedAt = f.CompletedAt
            }).ToList()
        };
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

        // Never block page open on Puppeteer/Chromium — HTML canvas always works.
        var existing = _visitingCardService.GetExistingCardPath(partner);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            ViewBag.CardImagePath = existing;
            ViewBag.CardImageUrl = $"{Request.Scheme}://{Request.Host}{existing}";
        }
        else
        {
            ViewBag.CardGenerateError =
                "PNG preview not created yet. Card design is ready on the left — click Download PNG to generate the image.";
        }

        return View(partner);
    }

    public async Task<IActionResult> DownloadVisitingCard()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        try
        {
            var webPath = await _visitingCardService.EnsureCardImageAsync(partner, forceRefresh: true);
            var physical = Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var png = await System.IO.File.ReadAllBytesAsync(physical);
            var safeName = string.Join("_", partner.CompanyName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "visiting-card";
            return File(png, "image/png", $"{safeName}-visiting-card.png");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Could not generate visiting-card PNG. Please try again in a minute.";
            return RedirectToAction(nameof(VisitingCard));
        }
    }

    public async Task<IActionResult> Certificate()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        var existing = _certificateService.GetExistingCertificatePath(partner);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            ViewBag.CertImagePath = existing;
        }
        else
        {
            ViewBag.CertGenerateError =
                "PNG not created yet. Certificate design is ready — click Download PNG to generate the image.";
        }

        return View(partner);
    }

    public async Task<IActionResult> IdCard()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        return View(partner);
    }

    public async Task<IActionResult> DownloadCertificate()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        try
        {
            var webPath = await _certificateService.EnsureCertificateImageAsync(partner, forceRefresh: true);
            var physical = Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var png = await System.IO.File.ReadAllBytesAsync(physical);
            var safeName = string.Join("_", (partner.OwnerName ?? partner.CompanyName)
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "partner-certificate";
            return File(png, "image/png", $"{safeName}-certificate.png");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Could not generate certificate PNG. Please try again in a minute.";
            return RedirectToAction(nameof(Certificate));
        }
    }

    public async Task<IActionResult> DownloadCertificatePdf()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        try
        {
            var pdf = await _certificateService.CreateCertificatePdfAsync(partner);
            var safeName = string.Join("_", (partner.OwnerName ?? partner.CompanyName)
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "partner-certificate";
            return File(pdf, "application/pdf", $"{safeName}-certificate.pdf");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Could not generate certificate PDF. Please try again in a minute.";
            return RedirectToAction(nameof(Certificate));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProposal(
        int clientId,
        string title,
        string scope,
        decimal newPrice,
        decimal? discountPercent = null,
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

        if (newPrice <= 0)
        {
            TempData["ErrorMessage"] = "Please enter a valid New Price.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        decimal? discount = null;
        if (discountPercent.HasValue && discountPercent.Value > 0)
        {
            if (discountPercent.Value > 100)
            {
                TempData["ErrorMessage"] = "Discount cannot be more than 100%.";
                return RedirectToAction(nameof(ClientDetails), new { id = clientId });
            }
            discount = Math.Round(discountPercent.Value, 2);
        }

        var finalAmount = discount.HasValue
            ? Math.Round(newPrice * (1 - discount.Value / 100m), 2)
            : Math.Round(newPrice, 2);

        if (finalAmount <= 0)
        {
            TempData["ErrorMessage"] = "Final amount after discount must be greater than zero.";
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
            OriginalAmount = Math.Round(newPrice, 2),
            DiscountPercent = discount,
            Amount = finalAmount,
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

        TempData["SuccessMessage"] = discount.HasValue
            ? $"Proposal created — New price ₹ {newPrice:N0}, discount {discount:N2}%, final ₹ {finalAmount:N0}."
            : $"Proposal created — New price ₹ {finalAmount:N0}.";
        return RedirectToAction(nameof(ClientDetails), new { id = clientId });
    }

    public async Task<IActionResult> DownloadProposal(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .Include(p => p.Service)
            .FirstOrDefaultAsync(p => p.Id == id && p.ChannelPartnerId == partner.Id);
        if (proposal == null) return NotFound();

        var pdf = await GetOrCreatePartnerProposalPdfAsync(proposal, partner);
        var fileName = BuildProposalFileName(proposal.Service?.Name, proposal.PartnerClient?.Name);
        return File(pdf, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendProposalEmail(int proposalId)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .Include(p => p.Service)
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
        var attachmentName = BuildProposalFileName(proposal.Service?.Name, client.Name);
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
              {(proposal.DiscountPercent is > 0 && proposal.OriginalAmount is > 0
                  ? $"New price: <strong>₹ {proposal.OriginalAmount:N2}</strong><br/>Discount: <strong>{proposal.DiscountPercent:N2}%</strong><br/>Final amount: <strong>₹ {proposal.Amount:N2}</strong><br/>"
                  : $"Amount: <strong>₹ {proposal.Amount:N2}</strong><br/>")}
              Valid until: <strong>{proposal.ValidUntil:dd MMM yyyy}</strong>
            </p>
            <p>Feel free to reply to this email or WhatsApp us if you have questions.</p>
            <p style='margin-top:24px'>Regards,<br/><strong>{System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(company.SignatoryName) ? company.CompanyName : company.SignatoryName)}</strong><br/>
            <span style='color:#6b7280;font-size:13px'>{System.Net.WebUtility.HtmlEncode(company.SignatoryTitle)}</span></p>
          </div>
        </div>";

        var ok = await _emailService.SendEmailAsync(client.Email, subject, html, pdf, attachmentName);
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
        OriginalAmount = proposal.OriginalAmount,
        DiscountPercent = proposal.DiscountPercent,
        TemplateKey = proposal.TemplateKey,
        ValidUntil = proposal.ValidUntil,
        CreatedAt = proposal.CreatedAt,
        ServiceCatalogId = proposal.ServiceCatalogId,
        SelectedModulesJson = proposal.SelectedModulesJson
    };

    /// <summary>Download/email file name: ServiceName_ClientName_proposal.pdf</summary>
    private static string BuildProposalFileName(string? serviceName, string? clientName)
    {
        static string Slug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Unknown";
            var cleaned = new string(value.Trim()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            while (cleaned.Contains("__", StringComparison.Ordinal))
                cleaned = cleaned.Replace("__", "_", StringComparison.Ordinal);
            cleaned = cleaned.Trim('_');
            if (cleaned.Length > 40) cleaned = cleaned[..40].TrimEnd('_');
            return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
        }

        return $"{Slug(serviceName)}_{Slug(clientName)}_proposal.pdf";
    }

    public async Task<IActionResult> EditProfile()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        ViewBag.Partner = partner;
        return View(partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> EditProfile(
        string? bankName,
        string? bankAccountName,
        string? bankAccountNumber,
        string? bankIfsc,
        string? bankBranch,
        string? upiId,
        string? upiName,
        IFormFile? photoFile,
        IFormFile? logoFile,
        IFormFile? qrFile,
        bool removePhoto = false,
        bool removeLogo = false,
        bool removeQr = false,
        string? currentPassword = null,
        string? newPassword = null,
        string? confirmPassword = null)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        // Only partner-managed fields — admin company/contact details stay locked.
        partner.BankName = TrimOrNull(bankName, 120);
        partner.BankAccountName = TrimOrNull(bankAccountName, 120);
        partner.BankAccountNumber = TrimOrNull(bankAccountNumber, 40);
        partner.BankIfsc = TrimOrNull(bankIfsc, 20)?.ToUpperInvariant();
        partner.BankBranch = TrimOrNull(bankBranch, 120);
        partner.UpiId = TrimOrNull(upiId, 100);
        partner.UpiName = TrimOrNull(upiName, 120);

        var changingPassword = !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(currentPassword);
        if (changingPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "Enter current and new password to change login password.";
                return View(partner);
            }
            if (newPassword.Trim().Length < 4)
            {
                TempData["ErrorMessage"] = "New password must be at least 4 characters.";
                return View(partner);
            }
            if (!string.Equals(newPassword.Trim(), (confirmPassword ?? "").Trim(), StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return View(partner);
            }
            var hash = partner.PasswordHash;
            if (!PasswordHelper.VerifyAndUpgrade(currentPassword, ref hash, out var upgraded))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return View(partner);
            }
            partner.PasswordHash = PasswordHelper.Hash(newPassword.Trim());
            partner.LoginPassword = newPassword.Trim();
        }

        try
        {
            if (removePhoto && !string.IsNullOrWhiteSpace(partner.PhotoPath))
            {
                TryDeletePartnerUpload(partner.PhotoPath);
                partner.PhotoPath = null;
            }

            if (photoFile is { Length: > 0 })
            {
                if (!string.IsNullOrWhiteSpace(partner.PhotoPath))
                    TryDeletePartnerUpload(partner.PhotoPath);
                partner.PhotoPath = await SavePartnerUploadAsync(photoFile, "photo", 2 * 1024 * 1024);
            }

            if (removeLogo && !string.IsNullOrWhiteSpace(partner.LogoPath))
            {
                TryDeletePartnerUpload(partner.LogoPath);
                partner.LogoPath = null;
            }

            if (logoFile is { Length: > 0 })
            {
                if (!string.IsNullOrWhiteSpace(partner.LogoPath))
                    TryDeletePartnerUpload(partner.LogoPath);
                partner.LogoPath = await SavePartnerUploadAsync(logoFile, "logo", 2 * 1024 * 1024);
            }

            if (removeQr && !string.IsNullOrWhiteSpace(partner.UpiQrPath))
            {
                TryDeletePartnerUpload(partner.UpiQrPath);
                partner.UpiQrPath = null;
            }

            if (qrFile is { Length: > 0 })
            {
                if (!string.IsNullOrWhiteSpace(partner.UpiQrPath))
                    TryDeletePartnerUpload(partner.UpiQrPath);
                partner.UpiQrPath = await SavePartnerUploadAsync(qrFile, "upi-qr", 2 * 1024 * 1024);
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(partner);
        }

        EnsureReferralCode(partner);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = changingPassword
            ? "Profile and password updated."
            : "Profile updated — photo, logo and payment details saved.";
        return RedirectToAction(nameof(EditProfile));
    }

    private static string? TrimOrNull(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > maxLen ? t[..maxLen] : t;
    }

    private async Task<string> SavePartnerUploadAsync(IFormFile file, string prefix, long maxBytes)
    {
        if (file.Length > maxBytes)
            throw new InvalidOperationException("Image must be under 2 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new InvalidOperationException("Image must be PNG, JPG, or WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners");
        Directory.CreateDirectory(dir);
        var name = $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(dir, name);
        await using var stream = System.IO.File.Create(full);
        await file.CopyToAsync(stream);
        return $"/uploads/partners/{name}";
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
        catch { /* ignore cleanup errors */ }
    }

    private async Task<ChannelPartner?> CurrentPartnerAsync()
    {
        var idClaim = User.FindFirst("PartnerId")?.Value;
        if (!int.TryParse(idClaim, out var id)) return null;
        var partner = await _context.ChannelPartners.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (partner != null && EnsureReferralCode(partner))
            await _context.SaveChangesAsync();
        return partner;
    }

    private static bool EnsureReferralCode(ChannelPartner partner)
    {
        if (!string.IsNullOrWhiteSpace(partner.ReferralCode)) return false;
        partner.ReferralCode = $"SF{partner.Id:D4}{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        return true;
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
