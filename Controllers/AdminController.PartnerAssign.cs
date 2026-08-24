using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;
using SoftflipSolutions.ViewModels;

namespace SoftflipSolutions.Controllers;

public partial class AdminController
{
    public async Task<IActionResult> PartnerLeadTracking()
    {
        var assigned = await _context.PartnerClients
            .AsNoTracking()
            .Include(c => c.ChannelPartner)
            .Where(c => c.SourceLeadType != null && c.SourceLeadId != null)
            .OrderByDescending(c => c.AssignedAt ?? c.CreatedAt)
            .ToListAsync();

        var clientIds = assigned.Select(c => c.Id).ToList();
        var followUps = await _context.FollowUpReminders
            .AsNoTracking()
            .Where(f => f.LeadType == LeadPipeline.LeadPartnerClient && clientIds.Contains(f.LeadId))
            .ToListAsync();

        var sourceKeys = assigned
            .Select(c => (c.SourceLeadType!, c.SourceLeadId!.Value))
            .Distinct()
            .ToList();
        var names = await ResolveLeadNamesAsync(sourceKeys);

        var rows = assigned.Select(c =>
        {
            var fus = followUps.Where(f => f.LeadId == c.Id).ToList();
            var open = fus.Where(f => !f.IsDone).OrderBy(f => f.DueAt).ToList();
            var latest = fus.OrderByDescending(f => f.CreatedAt).FirstOrDefault();
            return new PartnerAssignedLeadRow
            {
                PartnerClientId = c.Id,
                ClientName = c.Name,
                Mobile = c.Mobile,
                Requirement = c.Requirement,
                ChannelPartnerId = c.ChannelPartnerId,
                PartnerCompany = c.ChannelPartner?.CompanyName ?? $"Partner #{c.ChannelPartnerId}",
                SourceLeadType = c.SourceLeadType!,
                SourceLeadId = c.SourceLeadId!.Value,
                SourceLeadName = names.GetValueOrDefault((c.SourceLeadType!, c.SourceLeadId!.Value), c.Name),
                AssignedAt = c.AssignedAt ?? c.CreatedAt,
                AssignNote = c.AssignNote,
                OpenFollowUps = open.Count,
                OverdueFollowUps = open.Count(f => f.DueAt < DateTime.Now),
                NextDueAt = open.FirstOrDefault()?.DueAt,
                LatestFollowUpNote = latest?.Note
            };
        }).ToList();

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignLeadToPartner(string leadType, int leadId, int channelPartnerId, string? assignNote)
    {
        if (!IsKnownLeadType(leadType))
            return BadRequest();

        var existing = await _context.PartnerClients
            .FirstOrDefaultAsync(c => c.SourceLeadType == leadType && c.SourceLeadId == leadId);
        if (existing != null)
        {
            TempData["ErrorMessage"] = "This lead is already assigned to a partner.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var partner = await _context.ChannelPartners.FirstOrDefaultAsync(p => p.Id == channelPartnerId && p.IsActive);
        if (partner == null)
        {
            TempData["ErrorMessage"] = "Select an active channel partner.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var contact = await GetLeadContactAsync(leadType, leadId);
        if (contact == null)
        {
            TempData["ErrorMessage"] = "Lead not found.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        var budget = leadType == LeadPipeline.LeadClient
            ? (await _context.ClientLeads.AsNoTracking().Where(c => c.Id == leadId).Select(c => c.Budget).FirstOrDefaultAsync())
            : null;

        var client = new PartnerClient
        {
            ChannelPartnerId = partner.Id,
            Name = contact.Name,
            Mobile = contact.Phone ?? "",
            Email = contact.Email,
            WhatsApp = contact.Phone,
            Requirement = string.IsNullOrWhiteSpace(contact.Requirement) ? "Assigned Softflip lead" : contact.Requirement,
            Budget = budget,
            CreatedAt = DateTime.Now,
            SourceLeadType = leadType,
            SourceLeadId = leadId,
            AssignedAt = DateTime.Now,
            AssignedBy = User.Identity?.Name,
            AssignNote = string.IsNullOrWhiteSpace(assignNote) ? null : assignNote.Trim()
        };

        _context.PartnerClients.Add(client);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Lead assigned to {partner.CompanyName}. Partner can now follow up from their panel.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignLeadFromPartner(string leadType, int leadId)
    {
        if (!IsKnownLeadType(leadType))
            return BadRequest();

        var client = await _context.PartnerClients
            .FirstOrDefaultAsync(c => c.SourceLeadType == leadType && c.SourceLeadId == leadId);
        if (client == null)
        {
            TempData["ErrorMessage"] = "No partner assignment found for this lead.";
            return RedirectToLeadDetails(leadType, leadId);
        }

        // Keep partner's client record; clear Softflip assignment link so lead can be reassigned.
        client.SourceLeadType = null;
        client.SourceLeadId = null;
        client.AssignedAt = null;
        client.AssignedBy = null;
        client.AssignNote = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Partner assignment removed. The partner still keeps the client record.";
        return RedirectToLeadDetails(leadType, leadId);
    }

    private async Task PopulatePartnerAssignPanelAsync(string leadType, int leadId, string leadName)
    {
        var assigned = await _context.PartnerClients
            .AsNoTracking()
            .Include(c => c.ChannelPartner)
            .FirstOrDefaultAsync(c => c.SourceLeadType == leadType && c.SourceLeadId == leadId);

        var panel = new LeadPartnerAssignPanelViewModel
        {
            LeadType = leadType,
            LeadId = leadId,
            LeadName = leadName
        };

        if (assigned != null)
        {
            panel.IsAssigned = true;
            panel.PartnerClientId = assigned.Id;
            panel.ChannelPartnerId = assigned.ChannelPartnerId;
            panel.PartnerCompanyName = assigned.ChannelPartner?.CompanyName;
            panel.PartnerOwnerName = assigned.ChannelPartner?.OwnerName;
            panel.AssignedAt = assigned.AssignedAt;
            panel.AssignedBy = assigned.AssignedBy;
            panel.AssignNote = assigned.AssignNote;

            var items = await _context.FollowUpReminders
                .AsNoTracking()
                .Where(f => f.LeadType == LeadPipeline.LeadPartnerClient && f.LeadId == assigned.Id)
                .OrderBy(f => f.IsDone)
                .ThenBy(f => f.DueAt)
                .ToListAsync();

            panel.PartnerFollowUps = items.Select(f => new FollowUpReminderItem
            {
                Id = f.Id,
                LeadType = f.LeadType,
                LeadId = f.LeadId,
                LeadName = assigned.Name,
                StepType = f.StepType,
                DueAt = f.DueAt,
                Note = f.Note,
                IsDone = f.IsDone,
                CreatedAt = f.CreatedAt,
                CompletedAt = f.CompletedAt
            }).ToList();
        }
        else
        {
            panel.Partners = await _context.ChannelPartners
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.CompanyName)
                .Select(p => new PartnerOption
                {
                    Id = p.Id,
                    Label = p.CompanyName + " · " + p.OwnerName
                })
                .ToListAsync();
        }

        ViewBag.PartnerAssignPanel = panel;
    }
}
