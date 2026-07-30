using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IEmployeeDocumentPdfService
{
    byte[] CreateDocumentPdf(
        Employee employee,
        EmployeeDocumentTemplate template,
        CompanyProfile company,
        IDictionary<string, string> extras,
        string renderedBody,
        string documentTitle);
}

public class EmployeeDocumentPdfService : IEmployeeDocumentPdfService
{
    private readonly IWebHostEnvironment _env;
    private static readonly Color Accent = Color.FromHex("00AEEF");
    private static readonly Color Navy = Color.FromHex("152238");
    private static readonly Color SoftBorder = Color.FromHex("DCE8F0");

    static EmployeeDocumentPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public EmployeeDocumentPdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] CreateDocumentPdf(
        Employee employee,
        EmployeeDocumentTemplate template,
        CompanyProfile company,
        IDictionary<string, string> extras,
        string renderedBody,
        string documentTitle)
    {
        var lines = renderedBody
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(l => l.TrimEnd())
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(24);
                page.MarginBottom(SoftflipLetterheadFooter.RequiredBottomMargin);
                page.MarginHorizontal(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.4f));

                page.Header().Element(c => ComposeLetterheadHeader(c, company));
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().AlignCenter().Text(documentTitle.ToUpperInvariant())
                        .FontSize(15).SemiBold().FontColor(Navy).LetterSpacing(0.04f);
                    col.Item().PaddingTop(4).AlignCenter().Width(56).Height(2.5f).Background(Accent);
                    col.Item().PaddingTop(14);

                    foreach (var raw in lines)
                    {
                        var line = raw.Trim();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            col.Item().PaddingTop(6);
                            continue;
                        }

                        if (IsSectionHeading(line))
                        {
                            col.Item().PaddingTop(10).Text(line).SemiBold().FontSize(11).FontColor(Accent);
                            continue;
                        }

                        if (IsSubHeading(line))
                        {
                            col.Item().PaddingTop(6).Text(line).SemiBold().FontSize(10.5f).FontColor(Navy);
                            continue;
                        }

                        if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
                        {
                            col.Item().PaddingTop(4).Text(line).SemiBold().FontSize(10.5f).FontColor(Navy);
                            continue;
                        }

                        if (line.Equals("OFFER LETTER", StringComparison.OrdinalIgnoreCase) ||
                            line.Equals(documentTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        col.Item().PaddingTop(1.5f).Text(line).FontSize(10).LineHeight(1.4f);
                    }
                });
                page.Footer().Element(c => SoftflipLetterheadFooter.Compose(c, company));
            });
        }).GeneratePdf();
    }

    private void ComposeLetterheadHeader(IContainer container, CompanyProfile company)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    var logo = ReadImageBytes(company.LogoPath)
                               ?? ReadImageBytes("~/admin/img/softflip-logo.png")
                               ?? ReadImageBytes("~/admin/img/letterhead/image1.png");
                    if (logo != null)
                        left.Item().Width(88).Height(56).Image(logo).FitArea();
                    else
                        left.Item().Text(company.CompanyName).SemiBold().FontSize(16).FontColor(Accent);
                });

                row.ConstantItem(220).AlignRight().AlignMiddle().Column(right =>
                {
                    right.Item().AlignRight().Text(company.CompanyName).SemiBold().FontSize(12).FontColor(Navy);
                    if (!string.IsNullOrWhiteSpace(company.Website))
                        right.Item().AlignRight().Text(NormalizeWebsite(company.Website)).FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(company.ContactPhone))
                        right.Item().AlignRight().Text(company.ContactPhone).FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(company.ContactEmail))
                        right.Item().AlignRight().Text(company.ContactEmail).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).Height(2.5f).Background(Accent);
            col.Item().Height(1).Background(Color.FromHex("A8E4F8"));
        });
    }

    private static bool IsSectionHeading(string line) =>
        Regex.IsMatch(line, @"^\d{1,2}\.\s+\S+") ||
        line.Equals("ACCEPTANCE BY EMPLOYEE", StringComparison.OrdinalIgnoreCase);

    private static bool IsSubHeading(string line) =>
        line is "Software Development" or "IT Coordination" or "General Responsibilities" or "Authorized Signatory";

    private static string NormalizeWebsite(string website)
    {
        var value = website.Trim();
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) value = value[8..];
        else if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) value = value[7..];
        return value.TrimEnd('/');
    }

    private byte[]? ReadImageBytes(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return null;
        try
        {
            var relative = webPath.Trim().Replace("~/", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath, relative);
            return File.Exists(full) ? File.ReadAllBytes(full) : null;
        }
        catch
        {
            return null;
        }
    }
}
