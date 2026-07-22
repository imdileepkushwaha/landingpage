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

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(34);
                page.MarginVertical(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.35f));

                page.Header().Element(c => ComposeBrandedHeader(c, company, "PROPOSAL", accent));
                page.Content().PaddingTop(10).Element(c =>
                {
                    c.Column(col =>
                    {
                        col.Spacing(14);

                        // Client + meta
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Background(softBg).Border(1).BorderColor(softBorder).Padding(0).Column(box =>
                            {
                                box.Item().Height(3).Background(accent);
                                box.Item().Padding(14).Column(inner =>
                                {
                                    inner.Item().Text("PREPARED FOR").FontSize(7.5f).FontColor(Colors.Grey.Medium).LetterSpacing(0.08f);
                                    inner.Item().PaddingTop(4).Text(clientName).SemiBold().FontSize(14).FontColor(navy);
                                    if (!string.IsNullOrWhiteSpace(clientEmail))
                                        inner.Item().PaddingTop(2).Text(clientEmail).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrWhiteSpace(clientPhone))
                                        inner.Item().Text(clientPhone).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrWhiteSpace(requirement))
                                    {
                                        inner.Item().PaddingTop(8).Row(req =>
                                        {
                                            req.ConstantItem(3).Background(accent);
                                            req.ConstantItem(8);
                                            req.RelativeItem().Text(requirement).FontSize(9).FontColor(Colors.Grey.Darken2);
                                        });
                                    }
                                });
                            });
                            row.ConstantItem(12);
                            row.ConstantItem(148).Border(1).BorderColor(accent).Padding(0).Column(meta =>
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

                        // Title
                        col.Item().Column(title =>
                        {
                            title.Item().Text(proposal.Title).FontSize(19).SemiBold().FontColor(navy);
                            title.Item().PaddingTop(6).Width(56).Height(3).Background(accent);
                        });

                        // Included modules
                        if (modules.Count > 0)
                            ComposeIncludedModules(col, modules, accent, navy, softBg, softBorder);

                        // Scope
                        col.Item().Column(scope =>
                        {
                            scope.Item().Row(h =>
                            {
                                h.ConstantItem(3).Background(accent);
                                h.ConstantItem(8);
                                h.RelativeItem().AlignMiddle().Text("Scope of work").SemiBold().FontSize(11).FontColor(accent);
                            });
                            scope.Item().PaddingTop(8).Background(Colors.White).Border(1).BorderColor(softBorder)
                                .Padding(12).Text(proposal.Scope).FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.5f);
                        });

                        // Investment
                        col.Item().Background(Color.FromHex("E8F8FE")).Border(1).BorderColor(Color.FromHex("A8E4F8")).Padding(0).Row(row =>
                        {
                            row.RelativeItem().Padding(14).Column(left =>
                            {
                                left.Item().Text("INVESTMENT").FontSize(7.5f).FontColor(Colors.Grey.Medium).LetterSpacing(0.08f);
                                left.Item().PaddingTop(3).Text("One-time project fee").FontSize(10).SemiBold().FontColor(navy);
                                left.Item().Text("Inclusive of selected modules above").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(168).Background(accent).Padding(14).AlignMiddle().AlignRight().Column(amt =>
                            {
                                amt.Item().AlignRight().Text("TOTAL").FontSize(7).FontColor(Color.FromHex("D6F2FC"));
                                amt.Item().AlignRight().Text($"₹ {proposal.Amount:N2}").SemiBold().FontSize(16).FontColor(Colors.White);
                            });
                        });

                        ComposePaymentDetails(col, company, accent);

                        // Contact + signature
                        col.Item().PaddingTop(10).BorderTop(1).BorderColor(softBorder).PaddingTop(14).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("Questions?").SemiBold().FontSize(10).FontColor(navy);
                                left.Item().PaddingTop(4).Text("We’re happy to walk you through this proposal.").FontSize(8).FontColor(Colors.Grey.Medium);
                                if (!string.IsNullOrWhiteSpace(company.ContactPerson))
                                    left.Item().PaddingTop(6).Text(company.ContactPerson).SemiBold().FontSize(9);
                                if (!string.IsNullOrWhiteSpace(company.ContactPhone))
                                    left.Item().Text($"Phone: {company.ContactPhone}").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(company.ContactWhatsApp))
                                    left.Item().Text($"WhatsApp: {company.ContactWhatsApp}").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(company.ContactEmail))
                                    left.Item().Text(company.ContactEmail).FontSize(9);
                            });

                            row.ConstantItem(180).AlignRight().Column(sig =>
                            {
                                sig.Item().AlignRight().Text("Authorized signature").FontSize(8).FontColor(Colors.Grey.Medium);
                                var sigBytes = ReadImageBytes(company.SignaturePath);
                                if (sigBytes != null)
                                    sig.Item().PaddingTop(4).AlignRight().Height(48).Width(140).Image(sigBytes).FitArea();
                                else
                                    sig.Item().PaddingTop(28).AlignRight().Width(140).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                                sig.Item().PaddingTop(4).AlignRight()
                                    .Text(string.IsNullOrWhiteSpace(company.SignatoryName) ? company.CompanyName : company.SignatoryName)
                                    .SemiBold().FontSize(10).FontColor(navy);
                                if (!string.IsNullOrWhiteSpace(company.SignatoryTitle))
                                    sig.Item().AlignRight().Text(company.SignatoryTitle).FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                });

                page.Footer().Element(c => ComposeFooter(c, company, $"Proposal · {proposal.CreatedAt:dd MMM yyyy}"));
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
                    title.RelativeItem().Text("Included panels").SemiBold().FontSize(11).FontColor(accent);
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
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Row(brand =>
                {
                    var logo = ReadImageBytes(company.LogoPath);
                    if (logo != null)
                    {
                        brand.ConstantItem(52).Height(44).Image(logo).FitArea();
                        brand.ConstantItem(10);
                    }
                    brand.RelativeItem().AlignMiddle().Column(text =>
                    {
                        text.Item().Text(company.CompanyName).SemiBold().FontSize(16).FontColor(Colors.Grey.Darken4);
                        if (!string.IsNullOrWhiteSpace(company.Tagline))
                            text.Item().Text(company.Tagline).FontSize(8).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrWhiteSpace(company.Address))
                            text.Item().Text(company.Address).FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
                row.ConstantItem(110).AlignRight().AlignMiddle().Text(docType).SemiBold().FontSize(16).FontColor(accent);
            });
            col.Item().PaddingTop(10).PaddingBottom(6).LineHorizontal(2).LineColor(accent);
            if (!string.IsNullOrWhiteSpace(company.Gstin) || !string.IsNullOrWhiteSpace(company.Website))
            {
                col.Item().Row(meta =>
                {
                    if (!string.IsNullOrWhiteSpace(company.Gstin))
                        meta.RelativeItem().Text($"GSTIN: {company.Gstin}").FontSize(7).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(company.Website))
                        meta.RelativeItem().AlignRight().Text(company.Website).FontSize(7).FontColor(Colors.Grey.Medium);
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container, CompanyProfile company, string rightText)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text(company.CompanyName).FontSize(7).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(rightText).FontSize(7).FontColor(Colors.Grey.Medium);
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
