using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.ViewModels;

namespace SoftflipSolutions.ViewComponents;

public class HeaderAlertsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public HeaderAlertsViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new HeaderAlertsViewModel();
        var since = DateTime.Now.AddDays(-30);

        var pendingEnquiries = await _context.Enquiries
            .Where(e => (e.Status == LeadPipeline.Pending || e.Status == "") && e.CreatedAt >= since)
            .OrderByDescending(e => e.CreatedAt)
            .Take(8)
            .ToListAsync();

        var pendingDemos = await _context.DemoRequests
            .Where(e => (e.Status == LeadPipeline.Pending || e.Status == "") && e.CreatedAt >= since)
            .OrderByDescending(e => e.CreatedAt)
            .Take(8)
            .ToListAsync();

        var pendingLeads = await _context.ClientLeads
            .Where(e => (e.Status == LeadPipeline.Pending || e.Status == "") && e.CreatedAt >= since)
            .OrderByDescending(e => e.CreatedAt)
            .Take(8)
            .ToListAsync();

        var unpaidInvoices = await _context.Invoices
            .Where(i => i.Status == "Unpaid" || i.Status == "Partial")
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        var overdueFollowUps = await _context.FollowUpReminders
            .Where(f => !f.IsDone && f.DueAt < DateTime.Today)
            .OrderBy(f => f.DueAt)
            .Take(5)
            .ToListAsync();

        var expiringProposals = await _context.Proposals
            .Where(p => p.ValidUntil >= DateTime.Now && p.ValidUntil <= DateTime.Now.AddDays(3))
            .OrderBy(p => p.ValidUntil)
            .Take(5)
            .ToListAsync();

        foreach (var e in pendingEnquiries)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"enq-pending-{e.Id}",
                Title = "New enquiry pending",
                Subtitle = $"{e.Name} · {e.Requirement}",
                TimeLabel = Relative(e.CreatedAt),
                Url = $"/Admin/EnquiryDetails/{e.Id}",
                Icon = "bi-envelope",
                Accent = "cyan",
                CreatedAt = e.CreatedAt
            });
        }

        foreach (var d in pendingDemos)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"demo-pending-{d.Id}",
                Title = "Demo request pending",
                Subtitle = $"{d.Name} · {d.CompanyName}",
                TimeLabel = Relative(d.CreatedAt),
                Url = $"/Admin/DemoRequestDetails/{d.Id}",
                Icon = "bi-laptop",
                Accent = "green",
                CreatedAt = d.CreatedAt
            });
        }

        foreach (var c in pendingLeads)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"lead-pending-{c.Id}",
                Title = "External lead pending",
                Subtitle = $"{c.Name} · {c.Source}",
                TimeLabel = Relative(c.CreatedAt),
                Url = $"/Admin/ClientLeadDetails/{c.Id}",
                Icon = "bi-people",
                Accent = "cyan",
                CreatedAt = c.CreatedAt
            });
        }

        foreach (var inv in unpaidInvoices)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"inv-unpaid-{inv.Id}",
                Title = inv.Status == "Partial" ? "Partial payment due" : "Invoice unpaid",
                Subtitle = $"{inv.InvoiceNumber} · Due ₹ {inv.Balance:N0}",
                TimeLabel = Relative(inv.CreatedAt),
                Url = LeadUrl(inv.LeadType, inv.LeadId),
                Icon = "bi-receipt",
                Accent = "warn",
                CreatedAt = inv.CreatedAt
            });
        }

        foreach (var f in overdueFollowUps)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"fu-overdue-{f.Id}",
                Title = "Follow-up overdue",
                Subtitle = Truncate(f.Note, 80),
                TimeLabel = f.DueAt.ToString("dd MMM"),
                Url = LeadUrl(f.LeadType, f.LeadId),
                Icon = "bi-alarm",
                Accent = "warn",
                CreatedAt = f.DueAt
            });
        }

        foreach (var p in expiringProposals)
        {
            model.Notifications.Add(new HeaderAlertItem
            {
                Id = $"prop-exp-{p.Id}",
                Title = "Proposal expiring soon",
                Subtitle = $"{p.Title} · till {p.ValidUntil:dd MMM}",
                TimeLabel = Relative(p.CreatedAt),
                Url = LeadUrl(p.LeadType, p.LeadId),
                Icon = "bi-hourglass-split",
                Accent = "warn",
                CreatedAt = p.CreatedAt
            });
        }

        model.Notifications = model.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(12)
            .ToList();

        // Messages = inbound client text from forms
        var enquiryMsgs = await _context.Enquiries
            .Where(e => e.CreatedAt >= since && e.Message != null && e.Message != "")
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .ToListAsync();

        var demoMsgs = await _context.DemoRequests
            .Where(e => e.CreatedAt >= since && e.Message != null && e.Message != "")
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .ToListAsync();

        foreach (var e in enquiryMsgs)
        {
            model.Messages.Add(new HeaderAlertItem
            {
                Id = $"msg-enq-{e.Id}",
                Title = e.Name,
                Subtitle = Truncate(e.Message, 90),
                TimeLabel = Relative(e.CreatedAt),
                Url = $"/Admin/EnquiryDetails/{e.Id}",
                Icon = "bi-chat-left-text",
                Accent = "cyan",
                CreatedAt = e.CreatedAt
            });
        }

        foreach (var d in demoMsgs)
        {
            model.Messages.Add(new HeaderAlertItem
            {
                Id = $"msg-demo-{d.Id}",
                Title = d.Name,
                Subtitle = Truncate(d.Message, 90),
                TimeLabel = Relative(d.CreatedAt),
                Url = $"/Admin/DemoRequestDetails/{d.Id}",
                Icon = "bi-chat-dots",
                Accent = "green",
                CreatedAt = d.CreatedAt
            });
        }

        model.Messages = model.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(12)
            .ToList();

        return View(model);
    }

    private static string LeadUrl(string leadType, int leadId) => leadType switch
    {
        LeadPipeline.LeadClient => $"/Admin/ClientLeadDetails/{leadId}",
        LeadPipeline.LeadDemo => $"/Admin/DemoRequestDetails/{leadId}",
        _ => $"/Admin/EnquiryDetails/{leadId}"
    };

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "No message text";
        var t = text.Trim().Replace("\r\n", " ").Replace("\n", " ");
        return t.Length <= max ? t : t[..(max - 1)] + "…";
    }

    private static string Relative(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dt.ToString("dd MMM");
    }
}
