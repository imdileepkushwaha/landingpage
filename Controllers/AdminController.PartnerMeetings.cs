using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Controllers;

public partial class AdminController
{
    public async Task<IActionResult> PartnerMeetings()
    {
        var meetings = await _context.PartnerMeetings
            .AsNoTracking()
            .Include(m => m.Assignments)
                .ThenInclude(a => a.ChannelPartner)
            .OrderByDescending(m => m.MeetingAt)
            .ToListAsync();
        return View(meetings);
    }

    public async Task<IActionResult> AddPartnerMeeting()
    {
        await PopulatePartnerMeetingFormAsync();
        return View(new PartnerMeeting
        {
            MeetingAt = DateTime.Today.AddDays(1).AddHours(11),
            IsActive = true,
            AssignToAllPartners = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPartnerMeeting(PartnerMeeting model, int[]? partnerIds, bool assignToAllPartners)
    {
        await PopulatePartnerMeetingFormAsync();
        ModelState.Remove(nameof(PartnerMeeting.Assignments));

        var selectedIds = (partnerIds ?? Array.Empty<int>()).Distinct().ToList();
        if (!assignToAllPartners && selectedIds.Count == 0)
            ModelState.AddModelError(string.Empty, "Select at least one partner, or choose All partners.");

        if (!ModelState.IsValid)
            return View(model);

        if (!assignToAllPartners)
        {
            var validIds = await _context.ChannelPartners
                .AsNoTracking()
                .Where(p => p.IsActive && selectedIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();
            if (validIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Select at least one active partner.");
                return View(model);
            }
            selectedIds = validIds;
        }

        model.Title = model.Title.Trim();
        model.MeetingLink = model.MeetingLink.Trim();
        model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        model.CreatedAt = DateTime.Now;
        model.CreatedBy = User.Identity?.Name;
        model.IsActive = true;
        model.AssignToAllPartners = assignToAllPartners;
        model.Assignments = new List<PartnerMeetingAssignment>();

        if (!assignToAllPartners)
        {
            foreach (var pid in selectedIds)
            {
                model.Assignments.Add(new PartnerMeetingAssignment { ChannelPartnerId = pid });
            }
        }

        _context.PartnerMeetings.Add(model);
        await _context.SaveChangesAsync();

        var who = assignToAllPartners ? "all partners" : $"{selectedIds.Count} partner(s)";
        TempData["SuccessMessage"] = $"Meeting published for {who} (visible until meeting date/time).";
        return RedirectToAction(nameof(PartnerMeetings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePartnerMeeting(int id)
    {
        var meeting = await _context.PartnerMeetings.FindAsync(id);
        if (meeting == null) return NotFound();
        meeting.IsActive = !meeting.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = meeting.IsActive ? "Meeting activated." : "Meeting deactivated.";
        return RedirectToAction(nameof(PartnerMeetings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePartnerMeeting(int id)
    {
        var meeting = await _context.PartnerMeetings.FindAsync(id);
        if (meeting == null) return NotFound();
        _context.PartnerMeetings.Remove(meeting);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Meeting deleted.";
        return RedirectToAction(nameof(PartnerMeetings));
    }

    private async Task PopulatePartnerMeetingFormAsync()
    {
        ViewBag.Partners = await _context.ChannelPartners
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CompanyName)
            .Select(p => new SoftflipSolutions.ViewModels.PartnerOption
            {
                Id = p.Id,
                CompanyName = p.CompanyName,
                OwnerName = p.OwnerName,
                Email = p.Email,
                Label = p.CompanyName + " · " + p.OwnerName + " · " + p.Email
            })
            .ToListAsync();
    }
}
