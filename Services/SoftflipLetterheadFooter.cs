using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

/// <summary>Softflip letterhead footer — address left, page number right.</summary>
public static class SoftflipLetterheadFooter
{
    private static readonly Color Accent = Color.FromHex("00AEEF");
    private static readonly Color AccentSoft = Color.FromHex("A8E4F8");
    private static readonly Color TextDark = Color.FromHex("374151");
    private static readonly Color TextMuted = Color.FromHex("9CA3AF");

    public const float RequiredBottomMargin = 36f;

    public static void Compose(IContainer container, CompanyProfile company)
    {
        var address = string.IsNullOrWhiteSpace(company.Address)
            ? "Flat Number 101, Ist Floor, Ram Rahim Apartment, Alambagh, Lucknow, 226005"
            : company.Address.Trim();

        container.Column(col =>
        {
            col.Item().Height(2f).Background(Accent);
            col.Item().Height(1f).Background(AccentSoft);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text(address)
                    .FontSize(7.5f)
                    .FontColor(TextDark)
                    .LineHeight(1.3f);

                row.ConstantItem(72).AlignRight().AlignMiddle().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(TextMuted);
                    text.CurrentPageNumber().FontSize(7.5f).SemiBold().FontColor(Accent);
                    text.Span(" / ").FontSize(7.5f).FontColor(TextMuted);
                    text.TotalPages().FontSize(7.5f).SemiBold().FontColor(TextDark);
                });
            });
        });
    }
}
