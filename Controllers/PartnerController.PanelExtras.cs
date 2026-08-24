using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

public partial class PartnerController
{
    // ─── My Proposals ───────────────────────────────────────────────
    public async Task<IActionResult> Proposals()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var items = await _context.PartnerProposals
            .AsNoTracking()
            .Include(p => p.PartnerClient)
            .Include(p => p.Service)
            .Where(p => p.ChannelPartnerId == partner.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    // ─── Earnings / Commission ──────────────────────────────────────
    public async Task<IActionResult> Earnings()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var proposals = await _context.PartnerProposals
            .AsNoTracking()
            .Include(p => p.PartnerClient)
            .Include(p => p.Service)
            .Where(p => p.ChannelPartnerId == partner.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var rows = proposals.Select(p =>
        {
            var rate = p.Service?.Commission ?? 0m;
            var commission = Math.Round(p.Amount * rate / 100m, 2);
            return new PartnerEarningRow(
                p.Id,
                p.Title,
                p.PartnerClient?.Name ?? "—",
                p.Amount,
                rate,
                commission,
                p.IsCommissionPaid,
                p.CommissionPaidAt,
                p.CreatedAt);
        }).ToList();

        ViewBag.TotalQuoted = rows.Sum(r => r.Amount);
        ViewBag.TotalCommission = rows.Sum(r => r.Commission);
        ViewBag.PaidCommission = rows.Where(r => r.IsPaid).Sum(r => r.Commission);
        ViewBag.PendingCommission = rows.Where(r => !r.IsPaid).Sum(r => r.Commission);
        return View(rows);
    }

    public record PartnerEarningRow(
        int ProposalId,
        string Title,
        string ClientName,
        decimal Amount,
        decimal RatePercent,
        decimal Commission,
        bool IsPaid,
        DateTime? PaidAt,
        DateTime CreatedAt);

    // ─── Pipeline board ─────────────────────────────────────────────
    public async Task<IActionResult> Pipeline()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var clients = await _context.PartnerClients
            .AsNoTracking()
            .Where(c => c.ChannelPartnerId == partner.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Stages = PartnerClientStages.All;
        return View(clients);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClientStage(int id, string stage)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var client = await _context.PartnerClients
            .FirstOrDefaultAsync(c => c.Id == id && c.ChannelPartnerId == partner.Id);
        if (client == null) return NotFound();

        client.Stage = PartnerClientStages.Normalize(stage);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{client.Name} moved to {client.Stage}.";
        return RedirectToAction(nameof(Pipeline));
    }

    // ─── Invoices ───────────────────────────────────────────────────
    public async Task<IActionResult> Invoices()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var items = await _context.PartnerInvoices
            .AsNoTracking()
            .Include(i => i.PartnerClient)
            .Where(i => i.ChannelPartnerId == partner.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> CreateInvoice(int? clientId = null, int? proposalId = null)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        ViewBag.Clients = await _context.PartnerClients
            .AsNoTracking()
            .Where(c => c.ChannelPartnerId == partner.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        PartnerProposal? proposal = null;
        if (proposalId.HasValue)
        {
            proposal = await _context.PartnerProposals
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == proposalId && p.ChannelPartnerId == partner.Id);
            if (proposal != null)
            {
                clientId = proposal.PartnerClientId;
                ViewBag.Proposal = proposal;
            }
        }

        ViewBag.SelectedClientId = clientId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(
        int clientId,
        string title,
        string? description,
        decimal amount,
        decimal cgst = 0,
        decimal sgst = 0,
        decimal igst = 0,
        string? hsnSac = null,
        int? proposalId = null)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var client = await _context.PartnerClients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.ChannelPartnerId == partner.Id);
        if (client == null)
        {
            TempData["ErrorMessage"] = "Client not found.";
            return RedirectToAction(nameof(CreateInvoice));
        }

        if (string.IsNullOrWhiteSpace(title) || amount <= 0)
        {
            TempData["ErrorMessage"] = "Title and a positive amount are required.";
            return RedirectToAction(nameof(CreateInvoice), new { clientId, proposalId });
        }

        if (proposalId.HasValue)
        {
            var owns = await _context.PartnerProposals
                .AnyAsync(p => p.Id == proposalId && p.ChannelPartnerId == partner.Id && p.PartnerClientId == clientId);
            if (!owns) proposalId = null;
        }

        var invoice = new PartnerInvoice
        {
            ChannelPartnerId = partner.Id,
            PartnerClientId = clientId,
            PartnerProposalId = proposalId,
            InvoiceNumber = await NextPartnerInvoiceNumberAsync(),
            Title = title.Trim(),
            Description = (description ?? "").Trim(),
            Amount = amount,
            Cgst = Math.Max(0, cgst),
            Sgst = Math.Max(0, sgst),
            Igst = Math.Max(0, igst),
            HsnSac = TrimOrNull(hsnSac, 20),
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        };

        _context.PartnerInvoices.Add(invoice);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created.";
        return RedirectToAction(nameof(Invoices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPartnerInvoicePaid(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var invoice = await _context.PartnerInvoices
            .FirstOrDefaultAsync(i => i.Id == id && i.ChannelPartnerId == partner.Id);
        if (invoice == null) return NotFound();

        invoice.AmountPaid = invoice.GrandTotal;
        invoice.Status = "Paid";
        invoice.PaidAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{invoice.InvoiceNumber} marked paid.";
        return RedirectToAction(nameof(Invoices));
    }

    public async Task<IActionResult> DownloadPartnerInvoice(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var invoice = await _context.PartnerInvoices
            .Include(i => i.PartnerClient)
            .FirstOrDefaultAsync(i => i.Id == id && i.ChannelPartnerId == partner.Id);
        if (invoice == null) return NotFound();

        var company = partner.ToCompanyProfile();
        var pdf = _dealPdfService.CreateInvoicePdf(
            invoice.ToPdfInvoice(),
            invoice.PartnerClient?.Name ?? "Client",
            invoice.PartnerClient?.Email,
            invoice.PartnerClient?.Mobile,
            company);
        return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    private async Task<string> NextPartnerInvoiceNumberAsync()
    {
        var prefix = $"PINV-{DateTime.Now:yyyyMM}-";
        var last = await _context.PartnerInvoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();
        var seq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    // ─── Notifications ──────────────────────────────────────────────
    public async Task<IActionResult> Notifications()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        await SeedSyntheticNotificationsAsync(partner);

        var items = await _context.PartnerNotifications
            .Where(n => n.ChannelPartnerId == partner.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var n = await _context.PartnerNotifications
            .FirstOrDefaultAsync(x => x.Id == id && x.ChannelPartnerId == partner.Id);
        if (n != null)
        {
            n.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var items = await _context.PartnerNotifications
            .Where(n => n.ChannelPartnerId == partner.Id && !n.IsRead)
            .ToListAsync();
        foreach (var n in items) n.IsRead = true;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Notifications));
    }

    private async Task SeedSyntheticNotificationsAsync(ChannelPartner partner)
    {
        var clientIds = await _context.PartnerClients
            .Where(c => c.ChannelPartnerId == partner.Id)
            .Select(c => c.Id)
            .ToListAsync();

        var overdue = await _context.FollowUpReminders.CountAsync(f =>
            f.LeadType == LeadPipeline.LeadPartnerClient
            && clientIds.Contains(f.LeadId)
            && !f.IsDone
            && f.DueAt < DateTime.Now);

        if (overdue > 0)
        {
            var already = await _context.PartnerNotifications.AnyAsync(n =>
                n.ChannelPartnerId == partner.Id
                && n.CreatedAt >= DateTime.Today
                && n.Title == "Overdue follow-ups");
            if (!already)
            {
                _context.PartnerNotifications.Add(new PartnerNotification
                {
                    ChannelPartnerId = partner.Id,
                    Title = "Overdue follow-ups",
                    Message = $"You have {overdue} overdue follow-up{(overdue == 1 ? "" : "s")}.",
                    Type = "Warning",
                    Url = "/Partner/FollowUps",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        var meetings = await GetActiveMeetingsForPartnerAsync(partner.Id);
        foreach (var m in meetings.Take(3))
        {
            var title = $"Meeting: {m.Title}";
            var exists = await _context.PartnerNotifications.AnyAsync(n =>
                n.ChannelPartnerId == partner.Id && n.Title == title && n.CreatedAt >= DateTime.Today.AddDays(-2));
            if (!exists)
            {
                _context.PartnerNotifications.Add(new PartnerNotification
                {
                    ChannelPartnerId = partner.Id,
                    Title = title,
                    Message = $"Scheduled {m.MeetingAt:dd MMM yyyy, hh:mm tt}",
                    Type = "Info",
                    Url = "/Partner/Meetings",
                    CreatedAt = DateTime.Now
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    // ─── Support tickets ────────────────────────────────────────────
    public async Task<IActionResult> Support()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var tickets = await _context.PartnerTickets
            .AsNoTracking()
            .Where(t => t.ChannelPartnerId == partner.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return View(tickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseTicket(string subject, string message)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        subject = (subject ?? "").Trim();
        message = (message ?? "").Trim();
        if (subject.Length < 3 || message.Length < 5)
        {
            TempData["ErrorMessage"] = "Please enter a subject and message.";
            return RedirectToAction(nameof(Support));
        }

        _context.PartnerTickets.Add(new PartnerTicket
        {
            ChannelPartnerId = partner.Id,
            Subject = subject.Length > 200 ? subject[..200] : subject,
            Message = message.Length > 4000 ? message[..4000] : message,
            Status = "Open",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Support ticket submitted — Softflip will reply soon.";
        return RedirectToAction(nameof(Support));
    }

    // ─── Marketing kit ──────────────────────────────────────────────
    public async Task<IActionResult> MarketingKit()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;

        var items = await _context.MarketingKitItems
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> DownloadMarketingKit(int id)
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));

        var item = await _context.MarketingKitItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (item == null) return NotFound();

        var relative = item.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(_env.WebRootPath, relative);
        if (!System.IO.File.Exists(full)) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(full);
        var name = string.IsNullOrWhiteSpace(item.FileName) ? Path.GetFileName(full) : item.FileName;
        var ct = string.IsNullOrWhiteSpace(item.ContentType) ? "application/octet-stream" : item.ContentType;
        return File(bytes, ct, name);
    }

    // ─── Referral / share ───────────────────────────────────────────
    public async Task<IActionResult> Referral()
    {
        var partner = await CurrentPartnerAsync();
        if (partner == null) return RedirectToAction(nameof(Login));
        ViewBag.Partner = partner;
        EnsureReferralCode(partner);
        await _context.SaveChangesAsync();

        var shareUrl = Url.Action("Partners", "Home", new { refCode = partner.ReferralCode }, Request.Scheme)
                       ?? $"{Request.Scheme}://{Request.Host}/Home/Partners?refCode={partner.ReferralCode}";
        ViewBag.ShareUrl = shareUrl;
        ViewBag.WaShare = $"https://api.whatsapp.com/send?text={Uri.EscapeDataString($"Connect with Softflip through {partner.CompanyName} — Authorized Technology Support Partner.\n{shareUrl}")}";
        return View();
    }
}
