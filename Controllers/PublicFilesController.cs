using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

/// <summary>
/// Public PDF links for WhatsApp / email (no login). Filenames already include a GUID.
/// Also regenerates the file if it was wiped from disk during deploy.
/// </summary>
[AllowAnonymous]
public class PublicFilesController : Controller
{
    private static readonly Regex PartnerProposalName = new(
        @"^pp-\d+-[a-f0-9]{32}\.pdf$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SoftflipProposalName = new(
        @"^proposal-\d+-[a-f0-9]{32}\.pdf$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly IDealPdfService _dealPdfService;
    private readonly IWebHostEnvironment _env;

    public PublicFilesController(
        ApplicationDbContext context,
        IDealPdfService dealPdfService,
        IWebHostEnvironment env)
    {
        _context = context;
        _dealPdfService = dealPdfService;
        _env = env;
    }

    [HttpGet("/uploads/partners/proposals/{fileName}")]
    public async Task<IActionResult> PartnerProposal(string fileName)
    {
        if (!PartnerProposalName.IsMatch(fileName ?? ""))
            return NotFound();

        var relative = $"/uploads/partners/proposals/{fileName}";
        var proposal = await _context.PartnerProposals
            .Include(p => p.PartnerClient)
            .Include(p => p.ChannelPartner)
            .FirstOrDefaultAsync(p => p.FilePath == relative);
        if (proposal?.PartnerClient == null || proposal.ChannelPartner == null)
        {
            // Fallback: serve file from disk if DB row missing
            var existing = MapWww(relative);
            if (existing != null && System.IO.File.Exists(existing))
                return InlinePdf(existing);
            return NotFound();
        }

        var company = proposal.ChannelPartner.ToCompanyProfile();
        var pdf = _dealPdfService.CreateProposalPdf(
            new Proposal
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
            },
            proposal.PartnerClient.Name,
            proposal.PartnerClient.Email,
            proposal.PartnerClient.WhatsApp ?? proposal.PartnerClient.Mobile,
            proposal.PartnerClient.Requirement,
            company);

        var physical = await WriteBytesAsync(relative, pdf);
        return InlinePdf(physical);
    }

    [HttpGet("/uploads/proposals/{fileName}")]
    public IActionResult SoftflipProposal(string fileName)
    {
        if (!SoftflipProposalName.IsMatch(fileName ?? ""))
            return NotFound();

        var relative = $"/uploads/proposals/{fileName}";
        var physical = MapWww(relative);
        if (physical != null && System.IO.File.Exists(physical))
            return InlinePdf(physical);

        return NotFound();
    }

    private string? MapWww(string publicPath)
    {
        var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        var root = Path.GetFullPath(_env.WebRootPath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;
        return full;
    }

    private async Task<string> WriteBytesAsync(string publicPath, byte[] bytes)
    {
        var physical = MapWww(publicPath)
            ?? throw new InvalidOperationException("Invalid upload path.");
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        await System.IO.File.WriteAllBytesAsync(physical, bytes);
        return physical;
    }

    private IActionResult InlinePdf(string physicalPath)
    {
        Response.Headers.CacheControl = "private, max-age=3600";
        // No download file name → inline, so WhatsApp / browser can open the PDF.
        return PhysicalFile(physicalPath, "application/pdf");
    }
}
