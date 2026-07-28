using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IDealPdfService
{
    byte[] CreateProposalPdf(Proposal proposal, string clientName, string? clientEmail, string? clientPhone, string requirement, CompanyProfile company);
    byte[] CreateInvoicePdf(Invoice invoice, string clientName, string? clientEmail, string? clientPhone, CompanyProfile company);
}

public class DealPdfService : IDealPdfService
{
    private readonly IWebHostEnvironment _env;

    static DealPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DealPdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] CreateProposalPdf(Proposal proposal, string clientName, string? clientEmail, string? clientPhone, string requirement, CompanyProfile company)
    {
        var template = ProposalTemplates.Get(proposal.TemplateKey);
        var hex = (template.Accent ?? "#00AEEF").TrimStart('#');
        if (hex.Length != 6) hex = "00AEEF";
        var accent = Color.FromHex(hex);
        var navy = Color.FromHex("152238");
        var softBg = Color.FromHex("F4F8FB");
        var softBorder = Color.FromHex("DCE8F0");
        var modules = ParseModules(proposal.SelectedModulesJson);
        var firstName = (clientName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? clientName;
        var reqLabel = string.IsNullOrWhiteSpace(requirement) ? "your project" : requirement;
        var signatory = string.IsNullOrWhiteSpace(company.SignatoryName) ? company.CompanyName : company.SignatoryName;
        var signatoryTitle = string.IsNullOrWhiteSpace(company.SignatoryTitle) ? "Authorized Signatory" : company.SignatoryTitle;

        return Document.Create(container =>
        {
            // ——— Page 1: cover letter / attachment ———
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Colors.Grey.Darken3).LineHeight(1.45f));

                page.Header().Element(c => ComposeBrandedHeader(c, company, "PROPOSAL", accent));
                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Item().AlignCenter().Text(proposal.Title).FontSize(16).SemiBold().FontColor(navy);
                    col.Item().PaddingTop(4).AlignCenter().Width(48).Height(2.5f).Background(accent);

                    col.Item().PaddingTop(28).Text($"Dear {firstName},").SemiBold().FontSize(12).FontColor(navy);

                    col.Item().PaddingTop(14).Text(text =>
                    {
                        text.Span("We are pleased to present our proposal for creating ");
                        text.Span(reqLabel).SemiBold().FontColor(Colors.Red.Medium);
                        text.Span(".");
                    });

                    col.Item().PaddingTop(12).Text(
                        "We are representing a team of incredibly creative and experienced professionals who provide cost effective multimedia and web solutions.");

                    col.Item().PaddingTop(12).Text(
                        $"This proposal is based on the information provided. The following pages contain our understanding and scope of the project with the proposed solution for {reqLabel}.");

                    col.Item().PaddingTop(12).Text(
                        "Thanks for providing us this opportunity; we look forward to hearing from you.");

                    col.Item().PaddingTop(22).Text("Sincerely,").FontSize(11);

                    // Name + contact (left) | Validity (right)
                    col.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(signatory).SemiBold().FontSize(12).FontColor(navy);
                            left.Item().Text($"({signatoryTitle})").FontSize(9).FontColor(Colors.Grey.Darken1);

                            left.Item().PaddingTop(14);
                            if (!string.IsNullOrWhiteSpace(company.ContactPhone))
                                ComposeIconDetail(left, PdfIcons.Phone, "#00AEEF", company.ContactPhone, navy, alignRight: false);
                            if (!string.IsNullOrWhiteSpace(company.ContactWhatsApp))
                                ComposeIconDetail(left, PdfIcons.WhatsApp, "#25D366", company.ContactWhatsApp, navy, alignRight: false);
                            if (!string.IsNullOrWhiteSpace(company.ContactEmail))
                                ComposeIconDetail(left, PdfIcons.Email, "#00AEEF", company.ContactEmail, navy, alignRight: false);
                            if (!string.IsNullOrWhiteSpace(company.Website))
                                ComposeIconDetail(left, PdfIcons.Globe, "#00AEEF", company.Website, accent, alignRight: false);
                        });

                        row.ConstantItem(16);
                        row.ConstantItem(150).Border(1).BorderColor(accent).Column(meta =>
                        {
                            meta.Item().Background(accent).PaddingVertical(8).PaddingHorizontal(12)
                                .Text("VALIDITY").FontSize(7.5f).FontColor(Colors.White).LetterSpacing(0.08f);
                            meta.Item().Padding(12).Column(m =>
                            {
                                m.Item().Text(proposal.ValidUntil.ToString("dd MMM yyyy")).SemiBold().FontSize(12).FontColor(navy);
                                m.Item().PaddingTop(10).Text("STYLE").FontSize(7).FontColor(Colors.Grey.Medium);
                                m.Item().Text(template.Name).SemiBold().FontSize(10).FontColor(accent);
                                m.Item().PaddingTop(8).Text("DATE").FontSize(7).FontColor(Colors.Grey.Medium);
                                m.Item().Text(proposal.CreatedAt.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                });
                page.Footer().Element(c => ComposeFooter(c, company, ""));
            });

            // ——— Page 2+: services ———
            if (modules.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(34);
                    page.MarginVertical(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.35f));

                    page.Header().Element(c => ComposeBrandedHeader(c, company, "PROPOSAL", accent));
                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().AlignCenter().Text("List Of Our Services").FontSize(16).SemiBold().FontColor(navy);
                        col.Item().PaddingTop(4).AlignCenter().Width(56).Height(2.5f).Background(accent);
                        col.Item().PaddingTop(16);
                        ComposeIncludedModules(col, modules, accent, navy, softBg, softBorder);
                    });
                    page.Footer().Element(c => ComposeFooter(c, company, ""));
                });
            }

            // ——— Commercial page ———
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(34);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.35f));

                page.Header().Element(c => ComposeBrandedHeader(c, company, "PROPOSAL", accent));
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);
                    col.Item().AlignCenter().Text("Commercial").FontSize(16).SemiBold().FontColor(navy);
                    col.Item().AlignCenter().Width(48).Height(2.5f).Background(accent);

                    if (!string.IsNullOrWhiteSpace(proposal.Scope))
                    {
                        col.Item().PaddingTop(8).Column(scope =>
                        {
                            scope.Item().Text("Scope of work").SemiBold().FontSize(11).FontColor(accent);
                            scope.Item().PaddingTop(6).Background(Colors.White).Border(1).BorderColor(softBorder)
                                .Padding(12).Text(proposal.Scope).FontSize(10).LineHeight(1.5f);
                        });
                    }

                    col.Item().Background(Color.FromHex("E8F8FE")).Border(1).BorderColor(Color.FromHex("A8E4F8")).Padding(0).Row(row =>
                    {
                        row.RelativeItem().Padding(14).Column(left =>
                        {
                            left.Item().Text("PROJECT COST").FontSize(7.5f).FontColor(Colors.Grey.Medium).LetterSpacing(0.08f);
                            left.Item().PaddingTop(3).Text("One-time project fee").FontSize(10).SemiBold().FontColor(navy);
                        });
                        row.ConstantItem(168).Background(accent).Padding(14).AlignMiddle().AlignRight().Column(amt =>
                        {
                            amt.Item().AlignRight().Text("TOTAL").FontSize(7).FontColor(Color.FromHex("D6F2FC"));
                            amt.Item().AlignRight().Text($"₹ {proposal.Amount:N2}").SemiBold().FontSize(16).FontColor(Colors.White);
                        });
                    });

                    col.Item().Column(pay =>
                    {
                        pay.Item().Text("Payment Schedule").SemiBold().FontSize(11).FontColor(accent);
                        pay.Item().PaddingTop(6).Text("• Advance: 20%").FontSize(10);
                        pay.Item().Text("• After providing initial credentials: 50%").FontSize(10);
                        pay.Item().Text("• After delivery: 30%").FontSize(10);
                    });

                    col.Item().Column(time =>
                    {
                        time.Item().Text("Time Schedule").SemiBold().FontSize(11).FontColor(accent);
                        time.Item().PaddingTop(6).Text("1) Design and development: 7 to 10 days.").FontSize(10);
                        time.Item().Text("2) Testing: up to 2 days.").FontSize(10);
                        time.Item().Text("3) Customization: 2 to 4 days.").FontSize(10);
                        time.Item().Text("4) Training: up to 2 days.").FontSize(10);
                    });

                    ComposePaymentDetails(col, company, accent);
                });
                page.Footer().Element(c => ComposeFooter(c, company, ""));
            });
        }).GeneratePdf();
    }

    private static void ComposeIncludedModules(
        ColumnDescriptor col,
        List<ProposalModuleSelection> modules,
        Color accent,
        Color navy,
        Color softBg,
        Color softBorder)
    {
        var totalSubs = modules.Sum(m => m.SubModules?.Count ?? 0);

        col.Item().Column(section =>
        {
            section.Item().Row(h =>
            {
                h.ConstantItem(3).Background(accent);
                h.ConstantItem(8);
                h.RelativeItem().AlignMiddle().Row(title =>
                {
                    title.RelativeItem().Text("Services & modules").SemiBold().FontSize(11).FontColor(accent);
                    title.ConstantItem(110).AlignRight().AlignMiddle()
                        .Text($"{modules.Count} panel{(modules.Count == 1 ? "" : "s")}" +
                              (totalSubs > 0 ? $" · {totalSubs} features" : ""))
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            section.Item().PaddingTop(8).Border(1).BorderColor(softBorder).Background(softBg).Padding(10).Column(list =>
            {
                list.Spacing(8);
                for (var i = 0; i < modules.Count; i++)
                {
                    var mod = modules[i];
                    var index = (i + 1).ToString("00");
                    list.Item().Background(Colors.White).Border(1).BorderColor(softBorder).Padding(0).Row(card =>
                    {
                        card.ConstantItem(4).Background(accent);
                        card.RelativeItem().Padding(10).Column(body =>
                        {
                            body.Item().Row(head =>
                            {
                                head.ConstantItem(28).Height(22).Background(Color.FromHex("E8F8FE"))
                                    .AlignCenter().AlignMiddle()
                                    .Text(index).SemiBold().FontSize(8).FontColor(accent);
                                head.ConstantItem(8);
                                head.RelativeItem().AlignMiddle().Text(mod.Name).SemiBold().FontSize(11).FontColor(navy);
                                if (mod.SubModules != null && mod.SubModules.Count > 0)
                                {
                                    head.ConstantItem(72).AlignRight().AlignMiddle()
                                        .Text($"{mod.SubModules.Count} sub-module{(mod.SubModules.Count == 1 ? "" : "s")}")
                                        .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                }
                            });

                            if (mod.SubModules != null && mod.SubModules.Count > 0)
                            {
                                body.Item().PaddingTop(8).Column(chips =>
                                {
                                    // Wrap chips in rows of up to 3
                                    const int perRow = 3;
                                    for (var s = 0; s < mod.SubModules.Count; s += perRow)
                                    {
                                        var slice = mod.SubModules.Skip(s).Take(perRow).ToList();
                                        chips.Item().PaddingBottom(s + perRow < mod.SubModules.Count ? 5 : 0).Row(chipRow =>
                                        {
                                            foreach (var sub in slice)
                                            {
                                                chipRow.RelativeItem().PaddingRight(5)
                                                    .Background(Color.FromHex("F0F7FB"))
                                                    .Border(1).BorderColor(Color.FromHex("D5E8F2"))
                                                    .PaddingVertical(4).PaddingHorizontal(7)
                                                    .Row(chip =>
                                                    {
                                                        chip.ConstantItem(5).Height(5).Background(accent);
                                                        chip.ConstantItem(5);
                                                        chip.RelativeItem().AlignMiddle()
                                                            .Text(sub).FontSize(8).FontColor(Colors.Grey.Darken3);
                                                    });
                                            }
                                            // Fill empty slots so row stays even
                                            for (var fill = slice.Count; fill < perRow; fill++)
                                                chipRow.RelativeItem().PaddingRight(5);
                                        });
                                    }
                                });
                            }
                            else
                            {
                                body.Item().PaddingTop(6)
                                    .Text("Full module included")
                                    .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                            }
                        });
                    });
                }
            });
        });
    }

    public byte[] CreateInvoicePdf(Invoice invoice, string clientName, string? clientEmail, string? clientPhone, CompanyProfile company)
    {
        var accent = Color.FromHex("00AEEF");
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeBrandedHeader(c, company, "INVOICE", accent));
                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Bill to").FontSize(8).FontColor(Colors.Grey.Medium);
                            left.Item().Text(clientName).SemiBold().FontSize(12);
                            if (!string.IsNullOrWhiteSpace(clientEmail)) left.Item().Text(clientEmail).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(clientPhone)) left.Item().Text(clientPhone).FontSize(9);
                        });
                        row.ConstantItem(180).AlignRight().Column(right =>
                        {
                            right.Item().Text(invoice.InvoiceNumber).SemiBold().FontSize(12);
                            right.Item().Text($"Date: {invoice.CreatedAt:dd MMM yyyy}").FontSize(9);
                            right.Item().Text(invoice.Status == "Paid" ? "PAID" : "UNPAID")
                                .SemiBold().FontSize(10)
                                .FontColor(invoice.Status == "Paid" ? Colors.Green.Darken2 : accent);
                        });
                    });

                    col.Item().Text(invoice.Title).FontSize(14).SemiBold();
                    if (!string.IsNullOrWhiteSpace(invoice.Description))
                        col.Item().Text(invoice.Description).LineHeight(1.4f);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Description").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).AlignRight().Text("Amount").SemiBold();
                        });
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(invoice.Title);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text($"₹ {invoice.Amount:N2}");
                    });

                    col.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Invoice total: ₹ {invoice.Amount:N2}").FontSize(10);
                        if (invoice.AmountPaid > 0)
                            totals.Item().Text($"Paid: ₹ {invoice.AmountPaid:N2}").FontSize(10).FontColor(Colors.Green.Darken2);
                        totals.Item().PaddingTop(2)
                            .Text(invoice.Status == "Paid"
                                ? "Status: PAID"
                                : $"Balance due: ₹ {invoice.Balance:N2}")
                            .SemiBold().FontSize(13).FontColor(invoice.Status == "Paid" ? Colors.Green.Darken2 : accent);
                    });

                    ComposePaymentDetails(col, company, accent);

                    col.Item().PaddingTop(20).AlignRight().Column(sig =>
                    {
                        var sigBytes = ReadImageBytes(company.SignaturePath);
                        if (sigBytes != null)
                            sig.Item().Height(44).Image(sigBytes).FitArea();
                        sig.Item().Text(string.IsNullOrWhiteSpace(company.SignatoryName) ? company.CompanyName : company.SignatoryName).SemiBold().FontSize(10);
                        sig.Item().Text(company.SignatoryTitle).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Footer().Element(c => ComposeFooter(c, company, invoice.InvoiceNumber));
            });
        }).GeneratePdf();
    }

    private static void ComposePaymentDetails(ColumnDescriptor col, CompanyProfile company, Color accent)
    {
        if (!company.HasBankDetails) return;

        col.Item().PaddingTop(10).Background(Color.FromHex("E8F8FE")).Border(1).BorderColor(Color.FromHex("A8E4F8")).Padding(12).Column(pay =>
        {
            pay.Item().Text("Payment details").SemiBold().FontSize(11).FontColor(accent);
            pay.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(bank =>
                {
                    bank.Item().Text("Bank transfer").FontSize(8).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(company.BankName))
                        bank.Item().Text(company.BankName).SemiBold().FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.BankAccountName))
                        bank.Item().Text($"A/c name: {company.BankAccountName}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.BankAccountNumber))
                        bank.Item().Text($"A/c no: {company.BankAccountNumber}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.BankIfsc))
                        bank.Item().Text($"IFSC: {company.BankIfsc}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.BankBranch))
                        bank.Item().Text($"Branch: {company.BankBranch}").FontSize(9);
                });

                if (!string.IsNullOrWhiteSpace(company.UpiId))
                {
                    row.ConstantItem(16);
                    row.RelativeItem().Column(upi =>
                    {
                        upi.Item().Text("UPI").FontSize(8).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(company.UpiName))
                            upi.Item().Text(company.UpiName).SemiBold().FontSize(9);
                        upi.Item().Text(company.UpiId).SemiBold().FontSize(10).FontColor(accent);
                    });
                }
            });
        });
    }

    private void ComposeBrandedHeader(IContainer container, CompanyProfile company, string docType, Color accent)
    {
        var navy = Color.FromHex("152238");
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left — logo, then Authorized Partner under logo (if partner)
                row.RelativeItem().Column(left =>
                {
                    var logo = ReadImageBytes(company.LogoPath)
                               ?? ReadImageBytes("~/images/sf-logo.png")
                               ?? ReadImageBytes("~/admin/img/softflip-logo.png");
                    if (logo != null)
                        left.Item().Width(100).Height(72).Image(logo).FitArea();

                    if (company.IsAuthorizedPartner)
                    {
                        left.Item().PaddingTop(6)
                            .Background(accent)
                            .PaddingVertical(4).PaddingHorizontal(10)
                            .Text("An Authorised Channel Partner")
                            .FontSize(8).SemiBold().FontColor(Colors.White);
                    }
                    else if (!string.IsNullOrWhiteSpace(docType)
                             && !docType.Equals("PROPOSAL", StringComparison.OrdinalIgnoreCase))
                    {
                        // Keep INVOICE (etc.) label; never show "PROPOSAL" in header
                        left.Item().PaddingTop(6).Text(docType).FontSize(9).FontColor(accent).SemiBold();
                    }
                });

                // Right — contact details with icons
                row.ConstantItem(195).AlignMiddle().Column(details =>
                {
                    if (!string.IsNullOrWhiteSpace(company.ContactPhone))
                        ComposeIconDetail(details, PdfIcons.Phone, "#00AEEF", company.ContactPhone, navy, alignRight: true);
                    if (!string.IsNullOrWhiteSpace(company.ContactWhatsApp))
                        ComposeIconDetail(details, PdfIcons.WhatsApp, "#25D366", company.ContactWhatsApp, navy, alignRight: true);
                    if (!string.IsNullOrWhiteSpace(company.ContactEmail))
                        ComposeIconDetail(details, PdfIcons.Email, "#00AEEF", company.ContactEmail, navy, alignRight: true);
                    if (!string.IsNullOrWhiteSpace(company.Website))
                        ComposeIconDetail(details, PdfIcons.Globe, "#00AEEF", company.Website, accent, alignRight: true);
                    if (!string.IsNullOrWhiteSpace(company.Gstin))
                        ComposeIconDetail(details, PdfIcons.Gst, "#6B7280", $"GSTIN: {company.Gstin}", Colors.Grey.Darken1, alignRight: true);
                });
            });

            col.Item().PaddingTop(8).Height(2.5f).Background(accent);
        });
    }

    private static void ComposeIconDetail(ColumnDescriptor col, string svg, string iconHex, string value, Color textColor, bool alignRight = true)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            if (alignRight)
                row.RelativeItem();
            row.ConstantItem(14).Height(14).Svg(svg.Replace("{{COLOR}}", iconHex));
            row.ConstantItem(6);
            row.AutoItem().AlignMiddle().Text(value).FontSize(8).FontColor(textColor);
        });
    }

    private static void ComposeFooter(IContainer container, CompanyProfile company, string rightText)
    {
        container.Column(col =>
        {
            col.Item().Height(1.5f).Background(Color.FromHex("00AEEF"));
            col.Item().PaddingTop(8).AlignCenter().Text(
                    string.IsNullOrWhiteSpace(company.Address) ? "" : company.Address)
                .FontSize(8).FontColor(Colors.Grey.Darken1).AlignCenter();
        });
    }

    private static List<ProposalModuleSelection> ParseModules(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<ProposalModuleSelection>();
        try
        {
            return JsonSerializer.Deserialize<List<ProposalModuleSelection>>(json) ?? new List<ProposalModuleSelection>();
        }
        catch
        {
            return new List<ProposalModuleSelection>();
        }
    }

    private byte[]? ReadImageBytes(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return null;
        var relative = webPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(_env.WebRootPath, relative);
        return File.Exists(full) ? File.ReadAllBytes(full) : null;
    }
}

/// <summary>Inline SVG icons for QuestPDF ({{COLOR}} replaced at render time).</summary>
internal static class PdfIcons
{
    public const string Phone =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="{{COLOR}}"><path d="M6.62 10.79a15.15 15.15 0 006.59 6.59l2.2-2.2a1 1 0 011.01-.24c1.12.37 2.33.57 3.58.57a1 1 0 011 1V20a1 1 0 01-1 1C10.4 21 3 13.6 3 4a1 1 0 011-1h3.5a1 1 0 011 1c0 1.25.2 2.46.57 3.58a1 1 0 01-.25 1.02l-2.2 2.19z"/></svg>""";

    public const string WhatsApp =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="{{COLOR}}"><path d="M12.04 2C6.58 2 2.13 6.45 2.13 11.91c0 1.89.5 3.66 1.37 5.19L2 22l5.05-1.32a9.86 9.86 0 004.99 1.27h.01c5.46 0 9.91-4.45 9.91-9.91S17.5 2 12.04 2zm5.77 14.05c-.24.68-1.4 1.25-1.93 1.33-.5.07-1.13.1-1.82-.11-.42-.13-.96-.31-1.65-.61-2.9-1.25-4.79-4.17-4.93-4.36-.14-.19-1.16-1.54-1.16-2.94s.73-2.08 1-2.36c.24-.27.54-.34.72-.34h.52c.17 0 .39-.06.61.47.24.55.8 1.95.87 2.09.07.14.12.3.02.48-.1.19-.14.3-.28.47-.14.16-.3.36-.42.49-.14.14-.28.29-.12.57.16.27.71 1.17 1.52 1.89 1.05.93 1.93 1.22 2.21 1.36.28.14.44.12.6-.07.17-.19.71-.83.9-1.11.19-.28.38-.23.64-.14.27.09 1.7.8 1.99.95.29.14.48.22.55.34.07.12.07.69-.17 1.37z"/></svg>""";

    public const string Email =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="{{COLOR}}"><path d="M20 4H4a2 2 0 00-2 2v12a2 2 0 002 2h16a2 2 0 002-2V6a2 2 0 00-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/></svg>""";

    public const string Globe =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="{{COLOR}}"><path d="M12 2a10 10 0 100 20 10 10 0 000-20zm6.93 6h-3.17a15.9 15.9 0 00-1.31-3.34A8.03 8.03 0 0118.93 8zM12 4c.7 0 1.86 1.5 2.5 4H9.5C10.14 5.5 11.3 4 12 4zM4.26 14a7.97 7.97 0 010-4h3.4c-.16.66-.26 1.32-.26 2s.1 1.34.26 2h-3.4zm.81 2h3.17c.3 1.2.74 2.35 1.31 3.34A8.03 8.03 0 015.07 16zM8.5 8H5.07a8.03 8.03 0 014.48-3.34A15.9 15.9 0 008.5 8zM12 20c-.7 0-1.86-1.5-2.5-4h5c-.64 2.5-1.8 4-2.5 4zm3.1-6H8.9A13.7 13.7 0 018.9 10h6.2c.16.66.2 1.32.2 2s-.04 1.34-.2 2zm.33 5.34c.57-.99 1.01-2.14 1.31-3.34h3.17a8.03 8.03 0 01-4.48 3.34zM16.34 14c.16-.66.26-1.32.26-2s-.1-1.34-.26-2h3.4a7.97 7.97 0 010 4h-3.4z"/></svg>""";

    public const string Gst =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="{{COLOR}}"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6zm4 18H6V4h7v5h5v11zm-6-1h2v-2h-2v2zm0-4h2v-2h-2v2zm0-4h2V9h-2v2z"/></svg>""";
}
