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
        ViewBag.OpenInvoices = await _context.Invoices.CountAsync(i => i.Status == "Unpaid" || i.Status == "Partial");
        ViewBag.OutstandingBalance = await _context.Invoices
            .Where(i => i.Status == "Unpaid" || i.Status == "Partial")
            .SumAsync(i => (decimal?)(i.Amount - i.AmountPaid)) ?? 0m;
        ViewBag.OverdueFollowUps = await _context.FollowUpReminders
            .CountAsync(f => !f.IsDone && f.DueAt < DateTime.Today);
        ViewBag.ExpiringProposals = await _context.Proposals
            .CountAsync(p => p.ValidUntil >= DateTime.Today && p.ValidUntil <= DateTime.Today.AddDays(3));
        ViewBag.PendingCommission = await (
            from p in _context.PartnerProposals.AsNoTracking()
            join s in _context.ServiceCatalogs.AsNoTracking() on p.ServiceCatalogId equals s.Id into sj
            from s in sj.DefaultIfEmpty()
            where !p.IsCommissionPaid
            select p.Amount * (s != null ? s.Commission : 0m) / 100m
        ).SumAsync();
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

    public async Task<IActionResult> Proposals(string? source)
    {
        var filter = (source ?? "all").Trim().ToLowerInvariant();
        ViewBag.ProposalSource = filter is "admin" or "partner" ? filter : "all";

        var rows = new List<AdminProposalListItem>();

        if (filter is "all" or "admin")
        {
            var adminProposals = await _context.Proposals
                .AsNoTracking()
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var names = await ResolveLeadNamesAsync(
                adminProposals.Select(p => (p.LeadType, p.LeadId)).Distinct().ToList());

            rows.AddRange(adminProposals.Select(p => new AdminProposalListItem
            {
                Id = p.Id,
                Source = "Admin",
                LeadName = names.GetValueOrDefault((p.LeadType, p.LeadId), $"#{p.LeadId}"),
                LeadType = p.LeadType,
                LeadId = p.LeadId,
                Title = p.Title,
                Amount = p.Amount,
                ValidUntil = p.ValidUntil,
                CreatedAt = p.CreatedAt,
                FilePath = p.FilePath,
                HasInvoice = p.Invoice != null
            }));
        }

        if (filter is "all" or "partner")
        {
            var partnerProposals = await _context.PartnerProposals
                .AsNoTracking()
                .Include(p => p.ChannelPartner)
                .Include(p => p.PartnerClient)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            rows.AddRange(partnerProposals.Select(p => new AdminProposalListItem
            {
                Id = p.Id,
                Source = "Partner",
                LeadName = p.PartnerClient?.Name ?? $"Client #{p.PartnerClientId}",
                LeadType = "PartnerClient",
                LeadId = p.PartnerClientId,
                PartnerName = p.ChannelPartner?.CompanyName,
                Title = p.Title,
                Amount = p.Amount,
                ValidUntil = p.ValidUntil,
                CreatedAt = p.CreatedAt,
                FilePath = p.FilePath,
                HasInvoice = false
            }));
        }

        ViewBag.AdminProposalCount = await _context.Proposals.CountAsync();
        ViewBag.PartnerProposalCount = await _context.PartnerProposals.CountAsync();

        return View(rows.OrderByDescending(r => r.CreatedAt).ToList());
    }

    public async Task<IActionResult> DownloadPartnerProposal(int id)
    {
        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .Include(p => p.ChannelPartner)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proposal?.PartnerClient == null || proposal.ChannelPartner == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(proposal.FilePath))
        {
            var physical = MapUploadPath(proposal.FilePath);
            if (physical != null && System.IO.File.Exists(physical))
                return PhysicalFile(physical, "application/pdf", $"Partner-Proposal-{proposal.Id}.pdf");
        }

        var company = proposal.ChannelPartner.ToCompanyProfile();
        var pdf = _dealPdfService.CreateProposalPdf(
            new Proposal
            {
                Id = proposal.Id,
                Title = proposal.Title,
                Scope = proposal.Scope,
                Amount = proposal.Amount,
                ValidUntil = proposal.ValidUntil,
                TemplateKey = proposal.TemplateKey,
                SelectedModulesJson = proposal.SelectedModulesJson,
                CreatedAt = proposal.CreatedAt
            },
            proposal.PartnerClient.Name,
            proposal.PartnerClient.Email,
            proposal.PartnerClient.WhatsApp ?? proposal.PartnerClient.Mobile,
            proposal.PartnerClient.Requirement,
            company);
        return File(pdf, "application/pdf", $"Partner-Proposal-{proposal.Id}.pdf");
    }

    public async Task<IActionResult> Invoices()
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var names = await ResolveLeadNamesAsync(
            invoices.Select(i => (i.LeadType, i.LeadId)).Distinct().ToList());

        var rows = invoices.Select(i => new AdminInvoiceListItem
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            LeadName = names.GetValueOrDefault((i.LeadType, i.LeadId), $"#{i.LeadId}"),
            LeadType = i.LeadType,
            LeadId = i.LeadId,
            Title = i.Title,
            Amount = i.Amount,
            AmountPaid = i.AmountPaid,
            Balance = i.Balance,
            Status = i.Status,
            CreatedAt = i.CreatedAt
        }).ToList();

        return View(rows);
    }

    public async Task<IActionResult> CommissionTracker()
    {
        var proposals = await _context.PartnerProposals
            .AsNoTracking()
            .Include(p => p.ChannelPartner)
            .Include(p => p.PartnerClient)
            .Include(p => p.Service)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var rows = proposals.Select(p =>
        {
            var pct = p.Service?.Commission ?? 0m;
            var commission = Math.Round(p.Amount * pct / 100m, 2);
            return new CommissionTrackerRow
            {
                ProposalId = p.Id,
                PartnerId = p.ChannelPartnerId,
                PartnerName = p.ChannelPartner?.CompanyName ?? $"#{p.ChannelPartnerId}",
                ClientId = p.PartnerClientId,
                ClientName = p.PartnerClient?.Name ?? $"#{p.PartnerClientId}",
                Title = p.Title,
                Amount = p.Amount,
                CommissionPercent = pct,
                EstimatedCommission = commission,
                IsPaid = p.IsCommissionPaid,
                PaidAt = p.CommissionPaidAt,
                CreatedAt = p.CreatedAt
            };
        }).ToList();

        var vm = new CommissionTrackerViewModel
        {
            TotalEstimated = rows.Sum(r => r.EstimatedCommission),
            TotalPending = rows.Where(r => !r.IsPaid).Sum(r => r.EstimatedCommission),
            TotalPaid = rows.Where(r => r.IsPaid).Sum(r => r.EstimatedCommission),
            PendingCount = rows.Count(r => !r.IsPaid),
            PaidCount = rows.Count(r => r.IsPaid),
            Rows = rows
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionPaid(int id)
    {
        var proposal = await _context.PartnerProposals.FindAsync(id);
        if (proposal == null) return NotFound();
        proposal.IsCommissionPaid = true;
        proposal.CommissionPaidAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Commission marked as paid.";
        return RedirectToAction(nameof(CommissionTracker));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionUnpaid(int id)
    {
        var proposal = await _context.PartnerProposals.FindAsync(id);
        if (proposal == null) return NotFound();
        proposal.IsCommissionPaid = false;
        proposal.CommissionPaidAt = null;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Commission marked as pending.";
        return RedirectToAction(nameof(CommissionTracker));
    }

    public async Task<IActionResult> FollowUps(string? show)
    {
        var includeDone = string.Equals(show, "done", StringComparison.OrdinalIgnoreCase);
        var query = _context.FollowUpReminders.AsNoTracking().AsQueryable();
        if (!includeDone)
            query = query.Where(f => !f.IsDone);

        var items = await query
            .OrderBy(f => f.IsDone)
            .ThenBy(f => f.DueAt)
            .Take(200)
            .ToListAsync();

        var names = await ResolveLeadNamesAsync(
            items.Select(f => (f.LeadType, f.LeadId)).Distinct().ToList());

        var rows = items.Select(f => new FollowUpReminderItem
        {
            Id = f.Id,
            LeadType = f.LeadType,
            LeadId = f.LeadId,
            LeadName = names.GetValueOrDefault((f.LeadType, f.LeadId), $"#{f.LeadId}"),
            DueAt = f.DueAt,
            Note = f.Note,
            IsDone = f.IsDone,
            CreatedAt = f.CreatedAt
        }).ToList();

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFollowUp(string leadType, int leadId, DateTime dueAt, string note)
    {
        if (!IsKnownLeadType(leadType) || string.IsNullOrWhiteSpace(note))
            return RedirectToLeadDetails(leadType, leadId);

        _context.FollowUpReminders.Add(new FollowUpReminder
        {
            LeadType = leadType,
            LeadId = leadId,
            DueAt = dueAt.Date,
            Note = note.Trim(),
            IsDone = false,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Follow-up scheduled.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFollowUp(int id)
    {
        var item = await _context.FollowUpReminders.FindAsync(id);
        if (item == null) return NotFound();
        item.IsDone = true;
        item.CompletedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Follow-up marked done.";

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && referer.Contains("/Admin/FollowUps", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(FollowUps));

        return RedirectToLeadDetails(item.LeadType, item.LeadId);
    }

    public async Task<IActionResult> Activity()
    {
        var items = new List<ActivityFeedItem>();
        var since = DateTime.Now.AddDays(-45);

        foreach (var n in await _context.EnquiryNotes.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = n.CreatedAt,
                Type = "Note",
                Title = "Enquiry note",
                Subtitle = Truncate(n.NoteText, 100),
                Icon = "bi-chat-left-text",
                Accent = "info",
                Url = Url.Action(nameof(EnquiryDetails), new { id = n.EnquiryId })!
            });
        }

        foreach (var n in await _context.ClientLeadNotes.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = n.CreatedAt,
                Type = "Note",
                Title = "Client lead note",
                Subtitle = Truncate(n.NoteText, 100),
                Icon = "bi-chat-left-text",
                Accent = "info",
                Url = Url.Action(nameof(ClientLeadDetails), new { id = n.ClientLeadId })!
            });
        }

        foreach (var n in await _context.DemoRequestNotes.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = n.CreatedAt,
                Type = "Note",
                Title = "Demo note",
                Subtitle = Truncate(n.NoteText, 100),
                Icon = "bi-chat-left-text",
                Accent = "success",
                Url = Url.Action(nameof(DemoRequestDetails), new { id = n.DemoRequestId })!
            });
        }

        foreach (var p in await _context.Proposals.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = p.CreatedAt,
                Type = "Proposal",
                Title = $"Proposal · {p.Title}",
                Subtitle = $"₹{p.Amount:N0} · {p.LeadType}",
                Icon = "bi-file-earmark-text",
                Accent = "primary",
                Url = LeadDetailsPath(p.LeadType, p.LeadId)
            });
        }

        foreach (var i in await _context.Invoices.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = i.CreatedAt,
                Type = "Invoice",
                Title = $"Invoice · {i.InvoiceNumber}",
                Subtitle = $"₹{i.Amount:N0} · {i.Status}",
                Icon = "bi-receipt",
                Accent = "warning",
                Url = LeadDetailsPath(i.LeadType, i.LeadId)
            });
        }

        foreach (var pay in await _context.InvoicePayments.AsNoTracking().Where(x => x.PaidAt >= since).OrderByDescending(x => x.PaidAt).Take(40).ToListAsync())
        {
            var inv = await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pay.InvoiceId);
            items.Add(new ActivityFeedItem
            {
                At = pay.PaidAt,
                Type = "Payment",
                Title = "Payment recorded",
                Subtitle = $"₹{pay.Amount:N0}" + (inv != null ? $" · {inv.InvoiceNumber}" : ""),
                Icon = "bi-cash-coin",
                Accent = "success",
                Url = inv == null ? "#" : LeadDetailsPath(inv.LeadType, inv.LeadId)
            });
        }

        foreach (var d in await _context.LeadDocuments.AsNoTracking().Where(x => x.UploadedAt >= since).OrderByDescending(x => x.UploadedAt).Take(30).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = d.UploadedAt,
                Type = "Document",
                Title = $"Document · {d.Title}",
                Subtitle = d.Category,
                Icon = "bi-folder",
                Accent = "info",
                Url = LeadDetailsPath(d.LeadType, d.LeadId)
            });
        }

        foreach (var p in await _context.PartnerProposals.AsNoTracking().Include(x => x.ChannelPartner).Include(x => x.PartnerClient)
                     .Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = p.CreatedAt,
                Type = "PartnerProposal",
                Title = $"Partner proposal · {p.Title}",
                Subtitle = $"{p.ChannelPartner?.CompanyName} → {p.PartnerClient?.Name} · ₹{p.Amount:N0}",
                Icon = "bi-shop",
                Accent = "success",
                Url = Url.Action(nameof(PartnerClientDetails), new { id = p.PartnerClientId })!
            });
        }

        foreach (var f in await _context.FollowUpReminders.AsNoTracking().Where(x => x.CreatedAt >= since).OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync())
        {
            items.Add(new ActivityFeedItem
            {
                At = f.CreatedAt,
                Type = "FollowUp",
                Title = f.IsDone ? "Follow-up completed" : "Follow-up scheduled",
                Subtitle = $"{f.DueAt:dd MMM} · {Truncate(f.Note, 80)}",
                Icon = "bi-alarm",
                Accent = f.IsDone ? "success" : "warning",
                Url = LeadDetailsPath(f.LeadType, f.LeadId)
            });
        }

        return View(items.OrderByDescending(i => i.At).Take(120).ToList());
    }

    public async Task<IActionResult> Search(string? q)
    {
        var query = (q ?? "").Trim();
        var vm = new GlobalSearchViewModel { Query = query };
        if (query.Length < 2)
            return View(vm);

        vm.Results = await RunGlobalSearchAsync(query, 40);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SearchSuggest(string? q)
    {
        var query = (q ?? "").Trim();
        if (query.Length < 2)
            return Json(Array.Empty<object>());

        var results = await RunGlobalSearchAsync(query, 12);
        return Json(results.Select(r => new { r.Category, r.Title, r.Subtitle, r.Url, r.Icon }));
    }

    [HttpGet]
    public async Task<IActionResult> CheckDuplicateLead(string? phone, string? email, string? excludeType, int? excludeId)
    {
        var matches = await FindDuplicateLeadsAsync(phone, email, excludeType, excludeId);
        return Json(matches.Select(m => new
        {
            m.SourceType,
            m.Id,
            m.Name,
            m.Phone,
            m.Email,
            m.Status,
            m.MatchOn,
            m.Url,
            CreatedAt = m.CreatedAt.ToString("dd MMM yyyy")
        }));
    }

    public async Task<IActionResult> MessageTemplates()
    {
        var list = await _context.MessageTemplates
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Channel)
            .ThenBy(t => t.Name)
            .ToListAsync();
        return View(list);
    }

    public IActionResult AddMessageTemplate() => View(new MessageTemplate());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessageTemplate(MessageTemplate model)
    {
        if (!ModelState.IsValid) return View(model);
        model.Channel = NormalizeTemplateChannel(model.Channel);
        model.CreatedAt = DateTime.Now;
        model.IsActive = true;
        _context.MessageTemplates.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Template saved.";
        return RedirectToAction(nameof(MessageTemplates));
    }

    public async Task<IActionResult> EditMessageTemplate(int id)
    {
        var t = await _context.MessageTemplates.FindAsync(id);
        return t == null ? NotFound() : View(t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMessageTemplate(int id, MessageTemplate model)
    {
        var t = await _context.MessageTemplates.FindAsync(id);
        if (t == null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        t.Name = model.Name.Trim();
        t.Channel = NormalizeTemplateChannel(model.Channel);
        t.Subject = string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim();
        t.Body = model.Body.Trim();
        t.IsActive = model.IsActive;
        t.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Template updated.";
        return RedirectToAction(nameof(MessageTemplates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessageTemplate(int id)
    {
        var t = await _context.MessageTemplates.FindAsync(id);
        if (t != null)
        {
            _context.MessageTemplates.Remove(t);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Template deleted.";
        }
        return RedirectToAction(nameof(MessageTemplates));
    }

    [HttpGet]
    public async Task<IActionResult> GetMessageTemplates(string? channel)
    {
        var q = _context.MessageTemplates.AsNoTracking().Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(channel))
        {
            var c = NormalizeTemplateChannel(channel);
            q = q.Where(t => t.Channel == c);
        }
        var list = await q.OrderBy(t => t.Name).Select(t => new
        {
            t.Id,
            t.Name,
            t.Channel,
            t.Subject,
            t.Body
        }).ToListAsync();
        return Json(list);
    }

    public async Task<IActionResult> SalesSummary(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
        var start = (from ?? DateTime.Today.AddDays(-30)).Date;
        if (start > end) (start, end) = (end.Date.AddDays(-30), end);

        var partnerProps = await _context.PartnerProposals
            .AsNoTracking()
            .Include(p => p.Service)
            .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
            .ToListAsync();

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CreatedAt >= start && i.CreatedAt <= end)
            .ToListAsync();

        var openInvoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status == "Unpaid" || i.Status == "Partial")
            .ToListAsync();

        var vm = new SalesSummaryReportViewModel
        {
            From = start,
            To = end.Date,
            NewEnquiries = await _context.Enquiries.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end),
            NewDemos = await _context.DemoRequests.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end),
            NewExternalLeads = await _context.ClientLeads.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end),
            ProposalsCreated = await _context.Proposals.CountAsync(p => p.CreatedAt >= start && p.CreatedAt <= end),
            PartnerProposalsCreated = partnerProps.Count,
            ProposalValue = await _context.Proposals.Where(p => p.CreatedAt >= start && p.CreatedAt <= end).SumAsync(p => (decimal?)p.Amount) ?? 0m,
            PartnerProposalValue = partnerProps.Sum(p => p.Amount),
            InvoicesCreated = invoices.Count,
            InvoiceAmount = invoices.Sum(i => i.Amount),
            AmountCollected = await _context.InvoicePayments
                .Where(p => p.PaidAt >= start && p.PaidAt <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            Outstanding = openInvoices.Sum(i => i.Balance),
            EstimatedCommission = partnerProps.Sum(p => Math.Round(p.Amount * (p.Service?.Commission ?? 0m) / 100m, 2)),
            CommissionPaid = partnerProps.Where(p => p.IsCommissionPaid).Sum(p => Math.Round(p.Amount * (p.Service?.Commission ?? 0m) / 100m, 2)),
            CommissionPending = partnerProps.Where(p => !p.IsCommissionPaid).Sum(p => Math.Round(p.Amount * (p.Service?.Commission ?? 0m) / 100m, 2))
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
        model.DemoLink = string.IsNullOrWhiteSpace(model.DemoLink) ? null : model.DemoLink.Trim();
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
        service.DemoLink = string.IsNullOrWhiteSpace(model.DemoLink) ? null : model.DemoLink.Trim();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnquiry(int id)
    {
        var enquiry = await _context.Enquiries
            .Include(e => e.Notes)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry == null)
        {
            TempData["ErrorMessage"] = "Enquiry not found or already deleted.";
            return RedirectToAction(nameof(Enquiries));
        }

        var name = enquiry.Name;
        await DeleteEnquiryCascadeAsync(enquiry);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Enquiry from \"{name}\" deleted.";
        return RedirectToAction(nameof(Enquiries));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteEnquiries(List<int>? ids)
    {
        ids = (ids ?? new List<int>()).Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            TempData["ErrorMessage"] = "Select at least one enquiry to delete.";
            return RedirectToAction(nameof(Enquiries));
        }

        var enquiries = await _context.Enquiries
            .Include(e => e.Notes)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        foreach (var enquiry in enquiries)
            await DeleteEnquiryCascadeAsync(enquiry);

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{enquiries.Count} enquiry(ies) deleted.";
        return RedirectToAction(nameof(Enquiries));
    }

    public async Task<IActionResult> DemoRequests()
    {
        var requests = await _context.DemoRequests.Where(e => e.Status == "Pending" || e.Status == "").OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDemoRequest(int id)
    {
        var request = await _context.DemoRequests
            .Include(e => e.Notes)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (request == null)
        {
            TempData["ErrorMessage"] = "Demo request not found or already deleted.";
            return RedirectToAction(nameof(DemoRequests));
        }

        var name = request.Name;
        await DeleteDemoCascadeAsync(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Demo request from \"{name}\" deleted.";
        return RedirectToAction(nameof(DemoRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteDemoRequests(List<int>? ids)
    {
        ids = (ids ?? new List<int>()).Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            TempData["ErrorMessage"] = "Select at least one demo request to delete.";
            return RedirectToAction(nameof(DemoRequests));
        }

        var requests = await _context.DemoRequests
            .Include(e => e.Notes)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        foreach (var request in requests)
            await DeleteDemoCascadeAsync(request);

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{requests.Count} demo request(s) deleted.";
        return RedirectToAction(nameof(DemoRequests));
    }

    public async Task<IActionResult> EnquiryDetails(int id)
    {
        var enquiry = await _context.Enquiries.Include(e => e.Notes.OrderByDescending(n => n.CreatedAt)).FirstOrDefaultAsync(e => e.Id == id);
        if (enquiry == null) return NotFound();
        await PopulateDealPanelAsync(LeadPipeline.LeadEnquiry, id, enquiry.Name, enquiry.Requirement, null);
        await PopulateDocumentsPanelAsync(LeadPipeline.LeadEnquiry, id, enquiry.Status);
        await PopulateFollowUpsPanelAsync(LeadPipeline.LeadEnquiry, id);
        ViewBag.DuplicateLeads = await FindDuplicateLeadsAsync(enquiry.Phone, enquiry.Email, LeadPipeline.LeadEnquiry, id);
        ViewBag.MessageTemplates = await _context.MessageTemplates.AsNoTracking()
            .Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();

        var req = (enquiry.Requirement ?? "").Trim();
        string? serviceDemoDetails = null;
        string? serviceDemoName = null;
        int? matchedServiceId = null;
        if (!string.IsNullOrWhiteSpace(req))
        {
            // Match by name first (even if DemoLink empty) so UI can explain why button is missing.
            var catalog = await _context.ServiceCatalogs.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            var matched = catalog.FirstOrDefault(s =>
                string.Equals(s.Name, req, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                matchedServiceId = matched.Id;
                serviceDemoName = matched.Name;
                if (!string.IsNullOrWhiteSpace(matched.DemoLink))
                    serviceDemoDetails = matched.DemoLink;
            }
        }

        ViewBag.ServiceDemoDetails = serviceDemoDetails;
        ViewBag.ServiceDemoName = serviceDemoName;
        ViewBag.MatchedServiceId = matchedServiceId;
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
        await PopulateDealPanelAsync(LeadPipeline.LeadDemo, id, request.Name, request.Requirement, null);
        await PopulateDocumentsPanelAsync(LeadPipeline.LeadDemo, id, request.Status);
        await PopulateFollowUpsPanelAsync(LeadPipeline.LeadDemo, id);
        ViewBag.DuplicateLeads = await FindDuplicateLeadsAsync(request.Phone, request.Email, LeadPipeline.LeadDemo, id);
        ViewBag.MessageTemplates = await _context.MessageTemplates.AsNoTracking()
            .Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
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
        if (request != null && (status == LeadPipeline.Confirmed || status == LeadPipeline.Rejected))
        {
            request.Status = status;
            await _context.SaveChangesAsync();
        }
        return status == LeadPipeline.Confirmed
            ? RedirectToAction(nameof(DemoRequestDetails), new { id })
            : RedirectToAction(nameof(RejectedClients));
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

        var duplicates = await FindDuplicateLeadsAsync(model.Mobile, model.Email, null, null);
        if (duplicates.Count > 0)
            ViewBag.DuplicateLeads = duplicates;

        if (!ModelState.IsValid)
        {
            await PopulateLeadSourcesAsync(sourceChoice);
            return View(model);
        }

        // Soft warning only — still allow save when duplicates exist
        model.Status = "Pending";
        model.CreatedAt = DateTime.Now;
        _context.ClientLeads.Add(model);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = duplicates.Count > 0
            ? "Client lead added. Note: possible duplicate contacts were found."
            : "Client lead added successfully.";
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
        await PopulateFollowUpsPanelAsync(LeadPipeline.LeadClient, id);
        ViewBag.DuplicateLeads = await FindDuplicateLeadsAsync(lead.Mobile, lead.Email, LeadPipeline.LeadClient, id);
        ViewBag.MessageTemplates = await _context.MessageTemplates.AsNoTracking()
            .Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
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

        TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created for ₹ {invoice.Amount:N2}.";
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
            TempData["SuccessMessage"] = $"₹ {amount:N2} confirmed as received. Invoice fully paid (₹ {invoice.Amount:N2}).";
        }
        else
        {
            invoice.Status = "Partial";
            invoice.PaidAt = null;
            TempData["SuccessMessage"] = $"₹ {amount:N2} confirmed as received. Balance due: ₹ {invoice.Balance:N2}.";
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

        TempData["SuccessMessage"] = remaining > 0
            ? $"₹ {remaining:N2} confirmed as received. Invoice {invoice.InvoiceNumber} is fully paid (₹ {invoice.Amount:N2})."
            : $"Invoice {invoice.InvoiceNumber} confirmed fully paid (₹ {invoice.Amount:N2}).";
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
            publicUrl = $"https://{Request.Host}{proposal.FilePath}";
        }
        else if (proposal != null)
        {
            publicUrl = Url.Action(nameof(DownloadProposal), "Admin", new { id = proposal.Id }, "https") ?? "";
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

    private async Task PopulateFollowUpsPanelAsync(string leadType, int leadId)
    {
        var items = await _context.FollowUpReminders
            .AsNoTracking()
            .Where(f => f.LeadType == leadType && f.LeadId == leadId)
            .OrderBy(f => f.IsDone)
            .ThenBy(f => f.DueAt)
            .Take(20)
            .ToListAsync();

        ViewBag.FollowUpsPanel = new LeadFollowUpsPanelViewModel
        {
            LeadType = leadType,
            LeadId = leadId,
            Items = items.Select(f => new FollowUpReminderItem
            {
                Id = f.Id,
                LeadType = f.LeadType,
                LeadId = f.LeadId,
                DueAt = f.DueAt,
                Note = f.Note,
                IsDone = f.IsDone,
                CreatedAt = f.CreatedAt
            }).ToList()
        };
    }

    private async Task<Dictionary<(string LeadType, int LeadId), string>> ResolveLeadNamesAsync(
        List<(string LeadType, int LeadId)> keys)
    {
        var map = new Dictionary<(string, int), string>();
        if (keys.Count == 0) return map;

        var enquiryIds = keys.Where(k => k.LeadType == LeadPipeline.LeadEnquiry).Select(k => k.LeadId).Distinct().ToList();
        var clientIds = keys.Where(k => k.LeadType == LeadPipeline.LeadClient).Select(k => k.LeadId).Distinct().ToList();
        var demoIds = keys.Where(k => k.LeadType == LeadPipeline.LeadDemo).Select(k => k.LeadId).Distinct().ToList();

        if (enquiryIds.Count > 0)
        {
            foreach (var e in await _context.Enquiries.AsNoTracking().Where(x => enquiryIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync())
                map[(LeadPipeline.LeadEnquiry, e.Id)] = e.Name;
        }
        if (clientIds.Count > 0)
        {
            foreach (var c in await _context.ClientLeads.AsNoTracking().Where(x => clientIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync())
                map[(LeadPipeline.LeadClient, c.Id)] = c.Name;
        }
        if (demoIds.Count > 0)
        {
            foreach (var d in await _context.DemoRequests.AsNoTracking().Where(x => demoIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync())
                map[(LeadPipeline.LeadDemo, d.Id)] = d.Name;
        }

        return map;
    }

    private string LeadDetailsPath(string leadType, int leadId) => leadType switch
    {
        LeadPipeline.LeadClient => Url.Action(nameof(ClientLeadDetails), new { id = leadId })!,
        LeadPipeline.LeadDemo => Url.Action(nameof(DemoRequestDetails), new { id = leadId })!,
        _ => Url.Action(nameof(EnquiryDetails), new { id = leadId })!
    };

    private IActionResult RedirectToLeadDetails(string leadType, int leadId) => leadType switch
    {
        LeadPipeline.LeadClient => RedirectToAction(nameof(ClientLeadDetails), new { id = leadId }),
        LeadPipeline.LeadDemo => RedirectToAction(nameof(DemoRequestDetails), new { id = leadId }),
        _ => RedirectToAction(nameof(EnquiryDetails), new { id = leadId })
    };

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim().Replace("\r\n", " ").Replace("\n", " ");
        return t.Length <= max ? t : t[..(max - 1)] + "…";
    }

    private static string NormalizeTemplateChannel(string? channel) =>
        string.Equals(channel, "Email", StringComparison.OrdinalIgnoreCase) ? "Email" : "WhatsApp";

    private static string NormalizePhoneDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length > 10 && digits.StartsWith("91"))
            digits = digits[^10..];
        return digits;
    }

    private async Task<List<DuplicateLeadMatch>> FindDuplicateLeadsAsync(
        string? phone, string? email, string? excludeType, int? excludeId)
    {
        var matches = new List<DuplicateLeadMatch>();
        var phoneDigits = NormalizePhoneDigits(phone);
        var emailNorm = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        if (phoneDigits.Length >= 8)
        {
            foreach (var e in await _context.Enquiries.AsNoTracking().ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadEnquiry && excludeId == e.Id) continue;
                if (NormalizePhoneDigits(e.Phone) == phoneDigits)
                {
                    matches.Add(new DuplicateLeadMatch
                    {
                        SourceType = LeadPipeline.LeadEnquiry,
                        Id = e.Id,
                        Name = e.Name,
                        Phone = e.Phone,
                        Email = e.Email,
                        Status = e.Status ?? "",
                        CreatedAt = e.CreatedAt,
                        MatchOn = "Phone",
                        Url = Url.Action(nameof(EnquiryDetails), new { id = e.Id })!
                    });
                }
            }
            foreach (var c in await _context.ClientLeads.AsNoTracking().ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadClient && excludeId == c.Id) continue;
                if (NormalizePhoneDigits(c.Mobile) == phoneDigits)
                {
                    matches.Add(new DuplicateLeadMatch
                    {
                        SourceType = LeadPipeline.LeadClient,
                        Id = c.Id,
                        Name = c.Name,
                        Phone = c.Mobile,
                        Email = c.Email,
                        Status = c.Status ?? "",
                        CreatedAt = c.CreatedAt,
                        MatchOn = "Phone",
                        Url = Url.Action(nameof(ClientLeadDetails), new { id = c.Id })!
                    });
                }
            }
            foreach (var d in await _context.DemoRequests.AsNoTracking().ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadDemo && excludeId == d.Id) continue;
                if (NormalizePhoneDigits(d.Phone) == phoneDigits)
                {
                    matches.Add(new DuplicateLeadMatch
                    {
                        SourceType = LeadPipeline.LeadDemo,
                        Id = d.Id,
                        Name = d.Name,
                        Phone = d.Phone,
                        Email = d.Email,
                        Status = d.Status ?? "",
                        CreatedAt = d.CreatedAt,
                        MatchOn = "Phone",
                        Url = Url.Action(nameof(DemoRequestDetails), new { id = d.Id })!
                    });
                }
            }
            foreach (var p in await _context.PartnerClients.AsNoTracking().ToListAsync())
            {
                if (NormalizePhoneDigits(p.Mobile) == phoneDigits || NormalizePhoneDigits(p.WhatsApp) == phoneDigits)
                {
                    matches.Add(new DuplicateLeadMatch
                    {
                        SourceType = "PartnerClient",
                        Id = p.Id,
                        Name = p.Name,
                        Phone = p.Mobile,
                        Email = p.Email,
                        Status = "Partner",
                        CreatedAt = p.CreatedAt,
                        MatchOn = "Phone",
                        Url = Url.Action(nameof(PartnerClientDetails), new { id = p.Id })!
                    });
                }
            }
        }

        if (!string.IsNullOrEmpty(emailNorm))
        {
            foreach (var e in await _context.Enquiries.AsNoTracking()
                         .Where(x => x.Email != null && x.Email.ToLower() == emailNorm).ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadEnquiry && excludeId == e.Id) continue;
                if (matches.Any(m => m.SourceType == LeadPipeline.LeadEnquiry && m.Id == e.Id)) continue;
                matches.Add(new DuplicateLeadMatch
                {
                    SourceType = LeadPipeline.LeadEnquiry,
                    Id = e.Id,
                    Name = e.Name,
                    Phone = e.Phone,
                    Email = e.Email,
                    Status = e.Status ?? "",
                    CreatedAt = e.CreatedAt,
                    MatchOn = "Email",
                    Url = Url.Action(nameof(EnquiryDetails), new { id = e.Id })!
                });
            }
            foreach (var c in await _context.ClientLeads.AsNoTracking()
                         .Where(x => x.Email != null && x.Email.ToLower() == emailNorm).ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadClient && excludeId == c.Id) continue;
                if (matches.Any(m => m.SourceType == LeadPipeline.LeadClient && m.Id == c.Id)) continue;
                matches.Add(new DuplicateLeadMatch
                {
                    SourceType = LeadPipeline.LeadClient,
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Mobile,
                    Email = c.Email,
                    Status = c.Status ?? "",
                    CreatedAt = c.CreatedAt,
                    MatchOn = "Email",
                    Url = Url.Action(nameof(ClientLeadDetails), new { id = c.Id })!
                });
            }
            foreach (var d in await _context.DemoRequests.AsNoTracking()
                         .Where(x => x.Email != null && x.Email.ToLower() == emailNorm).ToListAsync())
            {
                if (excludeType == LeadPipeline.LeadDemo && excludeId == d.Id) continue;
                if (matches.Any(m => m.SourceType == LeadPipeline.LeadDemo && m.Id == d.Id)) continue;
                matches.Add(new DuplicateLeadMatch
                {
                    SourceType = LeadPipeline.LeadDemo,
                    Id = d.Id,
                    Name = d.Name,
                    Phone = d.Phone,
                    Email = d.Email,
                    Status = d.Status ?? "",
                    CreatedAt = d.CreatedAt,
                    MatchOn = "Email",
                    Url = Url.Action(nameof(DemoRequestDetails), new { id = d.Id })!
                });
            }
        }

        return matches.OrderByDescending(m => m.CreatedAt).Take(20).ToList();
    }

    private async Task<List<GlobalSearchResultItem>> RunGlobalSearchAsync(string query, int take)
    {
        var results = new List<GlobalSearchResultItem>();
        var q = query.Trim();
        var like = q.ToLowerInvariant();
        var phoneDigits = NormalizePhoneDigits(q);

        foreach (var e in await _context.Enquiries.AsNoTracking()
                     .Where(x => x.Name.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q) || x.Requirement.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Enquiry",
                Title = e.Name,
                Subtitle = $"{e.Phone} · {e.Requirement}",
                Url = Url.Action(nameof(EnquiryDetails), new { id = e.Id })!,
                Icon = "bi-envelope"
            });
        }

        foreach (var c in await _context.ClientLeads.AsNoTracking()
                     .Where(x => x.Name.Contains(q) || (x.Email != null && x.Email.Contains(q)) || x.Mobile.Contains(q) || x.Requirement.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "External Client",
                Title = c.Name,
                Subtitle = $"{c.Mobile} · {c.Source}",
                Url = Url.Action(nameof(ClientLeadDetails), new { id = c.Id })!,
                Icon = "bi-people"
            });
        }

        foreach (var d in await _context.DemoRequests.AsNoTracking()
                     .Where(x => x.Name.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q) || x.CompanyName.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Demo",
                Title = d.Name,
                Subtitle = d.CompanyName,
                Url = Url.Action(nameof(DemoRequestDetails), new { id = d.Id })!,
                Icon = "bi-laptop"
            });
        }

        foreach (var p in await _context.ChannelPartners.AsNoTracking()
                     .Where(x => x.CompanyName.Contains(q) || x.OwnerName.Contains(q) || x.Email.Contains(q) || x.Mobile.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Partner",
                Title = p.CompanyName,
                Subtitle = p.OwnerName,
                Url = Url.Action(nameof(ChannelPartnerDetails), new { id = p.Id })!,
                Icon = "bi-shop"
            });
        }

        foreach (var c in await _context.PartnerClients.AsNoTracking()
                     .Where(x => x.Name.Contains(q) || x.Mobile.Contains(q) || (x.Email != null && x.Email.Contains(q)))
                     .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Partner Client",
                Title = c.Name,
                Subtitle = c.Mobile,
                Url = Url.Action(nameof(PartnerClientDetails), new { id = c.Id })!,
                Icon = "bi-people-fill"
            });
        }

        foreach (var i in await _context.Invoices.AsNoTracking()
                     .Where(x => x.InvoiceNumber.Contains(q) || x.Title.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Invoice",
                Title = i.InvoiceNumber,
                Subtitle = $"{i.Title} · ₹{i.Amount:N0}",
                Url = LeadDetailsPath(i.LeadType, i.LeadId),
                Icon = "bi-receipt"
            });
        }

        foreach (var p in await _context.Proposals.AsNoTracking()
                     .Where(x => x.Title.Contains(q))
                     .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync())
        {
            results.Add(new GlobalSearchResultItem
            {
                Category = "Proposal",
                Title = p.Title,
                Subtitle = $"₹{p.Amount:N0}",
                Url = LeadDetailsPath(p.LeadType, p.LeadId),
                Icon = "bi-file-earmark-text"
            });
        }

        if (phoneDigits.Length >= 8)
        {
            // Extra phone pass already partly covered by Contains; keep list lean
        }

        _ = like; // reserved for future ranking
        return results.Take(take).ToList();
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

    private async Task DeleteEnquiryCascadeAsync(Enquiry enquiry)
    {
        var leadType = LeadPipeline.LeadEnquiry;
        var id = enquiry.Id;

        var followUps = await _context.FollowUpReminders
            .Where(f => f.LeadType == leadType && f.LeadId == id)
            .ToListAsync();
        if (followUps.Count > 0)
            _context.FollowUpReminders.RemoveRange(followUps);

        var docs = await _context.LeadDocuments
            .Where(d => d.LeadType == leadType && d.LeadId == id)
            .ToListAsync();
        foreach (var doc in docs)
        {
            var physical = MapUploadPath(doc.FilePath);
            if (physical != null && System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
            _context.LeadDocuments.Remove(doc);
        }

        var proposals = await _context.Proposals
            .Include(p => p.Invoice!)
                .ThenInclude(i => i.Payments)
            .Where(p => p.LeadType == leadType && p.LeadId == id)
            .ToListAsync();

        foreach (var proposal in proposals)
        {
            if (proposal.Invoice != null)
            {
                if (proposal.Invoice.Payments?.Count > 0)
                    _context.InvoicePayments.RemoveRange(proposal.Invoice.Payments);
                _context.Invoices.Remove(proposal.Invoice);
            }
            var pdfPath = MapUploadPath(proposal.FilePath);
            if (pdfPath != null && System.IO.File.Exists(pdfPath))
                System.IO.File.Delete(pdfPath);
            _context.Proposals.Remove(proposal);
        }

        var orphanInvoices = await _context.Invoices
            .Include(i => i.Payments)
            .Where(i => i.LeadType == leadType && i.LeadId == id)
            .ToListAsync();
        foreach (var inv in orphanInvoices)
        {
            if (inv.Payments?.Count > 0)
                _context.InvoicePayments.RemoveRange(inv.Payments);
            _context.Invoices.Remove(inv);
        }

        if (enquiry.Notes.Count > 0)
            _context.EnquiryNotes.RemoveRange(enquiry.Notes);

        _context.Enquiries.Remove(enquiry);
    }

    private async Task DeleteDemoCascadeAsync(DemoRequest request)
    {
        var leadType = LeadPipeline.LeadDemo;
        var id = request.Id;

        var followUps = await _context.FollowUpReminders
            .Where(f => f.LeadType == leadType && f.LeadId == id)
            .ToListAsync();
        if (followUps.Count > 0)
            _context.FollowUpReminders.RemoveRange(followUps);

        var docs = await _context.LeadDocuments
            .Where(d => d.LeadType == leadType && d.LeadId == id)
            .ToListAsync();
        foreach (var doc in docs)
        {
            var physical = MapUploadPath(doc.FilePath);
            if (physical != null && System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
            _context.LeadDocuments.Remove(doc);
        }

        var proposals = await _context.Proposals
            .Include(p => p.Invoice!)
                .ThenInclude(i => i.Payments)
            .Where(p => p.LeadType == leadType && p.LeadId == id)
            .ToListAsync();

        foreach (var proposal in proposals)
        {
            if (proposal.Invoice != null)
            {
                if (proposal.Invoice.Payments?.Count > 0)
                    _context.InvoicePayments.RemoveRange(proposal.Invoice.Payments);
                _context.Invoices.Remove(proposal.Invoice);
            }
            var pdfPath = MapUploadPath(proposal.FilePath);
            if (pdfPath != null && System.IO.File.Exists(pdfPath))
                System.IO.File.Delete(pdfPath);
            _context.Proposals.Remove(proposal);
        }

        var orphanInvoices = await _context.Invoices
            .Include(i => i.Payments)
            .Where(i => i.LeadType == leadType && i.LeadId == id)
            .ToListAsync();
        foreach (var inv in orphanInvoices)
        {
            if (inv.Payments?.Count > 0)
                _context.InvoicePayments.RemoveRange(inv.Payments);
            _context.Invoices.Remove(inv);
        }

        if (request.Notes.Count > 0)
            _context.DemoRequestNotes.RemoveRange(request.Notes);

        _context.DemoRequests.Remove(request);
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
