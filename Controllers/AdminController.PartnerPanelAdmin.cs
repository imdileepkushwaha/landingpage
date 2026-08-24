using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

public partial class AdminController
{
    // ─── Partner support tickets ────────────────────────────────────
    public async Task<IActionResult> PartnerTickets(string? status = null)
    {
        var q = _context.PartnerTickets.AsNoTracking().Include(t => t.ChannelPartner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(t => t.Status == status);
        var items = await q.OrderByDescending(t => t.CreatedAt).Take(200).ToListAsync();
        ViewBag.Status = status;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyPartnerTicket(int id, string? adminReply, string status = "InProgress")
    {
        var ticket = await _context.PartnerTickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.AdminReply = string.IsNullOrWhiteSpace(adminReply) ? ticket.AdminReply : adminReply.Trim();
        ticket.Status = status is "Open" or "InProgress" or "Resolved" or "Closed" ? status : "InProgress";
        ticket.UpdatedAt = DateTime.Now;
        if (ticket.Status is "Resolved" or "Closed")
            ticket.ResolvedAt = DateTime.Now;

        _context.PartnerNotifications.Add(new PartnerNotification
        {
            ChannelPartnerId = ticket.ChannelPartnerId,
            Title = "Support ticket update",
            Message = $"Ticket \"{ticket.Subject}\" is now {ticket.Status}.",
            Type = "Info",
            Url = "/Partner/Support",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Ticket updated.";
        return RedirectToAction(nameof(PartnerTickets));
    }

    // ─── Marketing kit ──────────────────────────────────────────────
    public async Task<IActionResult> MarketingKitAdmin()
    {
        var items = await _context.MarketingKitItems
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> AddMarketingKitItem(
        string title,
        string? description,
        string category,
        IFormFile file,
        int sortOrder = 0)
    {
        title = (title ?? "").Trim();
        if (title.Length < 2 || file is not { Length: > 0 })
        {
            TempData["ErrorMessage"] = "Title and file are required.";
            return RedirectToAction(nameof(MarketingKitAdmin));
        }

        if (file.Length > 15 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File must be under 15 MB.";
            return RedirectToAction(nameof(MarketingKitAdmin));
        }

        var dir = Path.Combine(_env.WebRootPath, "uploads", "marketing-kit");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName);
        var stored = $"kit-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(dir, stored);
        await using (var stream = System.IO.File.Create(full))
            await file.CopyToAsync(stream);

        _context.MarketingKitItems.Add(new MarketingKitItem
        {
            Title = title.Length > 200 ? title[..200] : title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Other" : category.Trim(),
            FilePath = $"/uploads/marketing-kit/{stored}",
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Marketing kit item added.";
        return RedirectToAction(nameof(MarketingKitAdmin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMarketingKitItem(int id)
    {
        var item = await _context.MarketingKitItems.FindAsync(id);
        if (item == null) return NotFound();
        item.IsActive = !item.IsActive;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MarketingKitAdmin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMarketingKitItem(int id)
    {
        var item = await _context.MarketingKitItems.FindAsync(id);
        if (item == null) return NotFound();
        var relative = item.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(_env.WebRootPath, relative);
        try
        {
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch { /* ignore */ }
        _context.MarketingKitItems.Remove(item);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Item deleted.";
        return RedirectToAction(nameof(MarketingKitAdmin));
    }
}
