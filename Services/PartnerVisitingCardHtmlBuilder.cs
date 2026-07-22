using System.Net;
using System.Text;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public static class PartnerVisitingCardHtmlBuilder
{
    private const string PhoneIcon = """<svg viewBox="0 0 16 16"><path d="M3.654 1.328a.678.678 0 0 1 .737-.064l2.79 1.395c.329.165.445.564.25.857l-1.015 1.738a.678.678 0 0 0-.033.635l.984 2.46a.678.678 0 0 0 .955.282l1.738-1.015a.678.678 0 0 1 .857.25l1.395 2.79a.678.678 0 0 1-.064.737l-1.272 1.272a2.5 2.5 0 0 1-3.182.066L2.328 10.654a2.5 2.5 0 0 1 .066-3.182L3.654 6.2z"/></svg>""";

    private const string LinkIcon = """<svg viewBox="0 0 16 16"><path d="M4.715 6.542 3.343 7.914a3 3 0 1 0 4.243 4.243l1.828-1.829A3 3 0 0 0 8.586 5.5L8 6.086a1 1 0 0 0-.154.199 2 2 0 0 1 .861 3.337L6.88 11.45a2 2 0 1 1-2.83-2.83l.793-.792a4 4 0 0 1-.128-1.287z"/><path d="M6.586 4.672A3 3 0 0 0 7.414 9.5l.775-.776a2 2 0 0 1-.896-3.346L9.12 4.55a2 2 0 1 1 2.83 2.83l-.793.792c.112.42.155.855.128 1.287l1.372-1.372a3 3 0 1 0-4.243-4.243L6.586 4.672z"/></svg>""";

    private const string EmailIcon = """<svg viewBox="0 0 16 16"><path d="M2 4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V4zm2-.5a.5.5 0 0 0-.5.5v.217l4.5 2.625 4.5-2.625V4a.5.5 0 0 0-.5-.5H4zm9 1.358-4.042 2.364a.5.5 0 0 1-.458 0L3 4.858V12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.858z"/></svg>""";

    private const string LocationIcon = """<svg viewBox="0 0 16 16"><path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/></svg>""";

    public static string Build(ChannelPartner partner, string webRootPath, string cssPath)
    {
        var owner = WebUtility.HtmlEncode((partner.OwnerName ?? "").Trim());
        var mobile = WebUtility.HtmlEncode((partner.Mobile ?? "").Trim());
        var email = WebUtility.HtmlEncode((partner.Email ?? "").Trim());
        var address = WebUtility.HtmlEncode(partner.FullAddress);
        var website = WebUtility.HtmlEncode(partner.DisplayWebsite);
        var company = (partner.CompanyName ?? "").Trim();

        var logoHtml = BuildLogoHtml(partner.LogoPath, company, webRootPath);
        var css = File.ReadAllText(cssPath);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        sb.Append(css);
        sb.Append("</style></head><body style=\"margin:0;padding:0;background:#fff;\">");
        sb.Append("<div class=\"vcard-canvas\">");
        sb.Append("<div class=\"vcard-top\"><div class=\"vcard-logo-wrap\">").Append(logoHtml).Append("</div></div>");
        sb.Append("<div class=\"vcard-divider\"></div>");
        sb.Append("<div class=\"vcard-body\">");
        if (!string.IsNullOrEmpty(owner))
            sb.Append("<div class=\"vcard-name\">").Append(owner).Append("</div>");
        sb.Append("<div class=\"vcard-role\">Owner</div>");
        sb.Append("<div class=\"vcard-contacts\">");

        if (!string.IsNullOrEmpty(mobile))
            sb.Append(ContactRow(PhoneIcon, mobile));

        if (!string.IsNullOrEmpty(website))
            sb.Append(ContactRow(LinkIcon, website));
        else if (!string.IsNullOrEmpty(email))
            sb.Append(ContactRow(EmailIcon, email));

        if (!string.IsNullOrEmpty(address))
            sb.Append(ContactRow(LocationIcon, address));

        sb.Append("</div></div></div></body></html>");
        return sb.ToString();
    }

    private static string ContactRow(string icon, string value) =>
        $"""<div class="vcard-contact-row"><span class="vcard-icon">{icon}</span><span class="vcard-contact-val">{value}</span></div>""";

    public static string BuildLogoHtml(string? logoPath, string company, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            var initial = string.IsNullOrWhiteSpace(company) ? "?" : WebUtility.HtmlEncode(company[..1].ToUpperInvariant());
            return $"""<span class="vcard-logo-fallback">{initial}</span>""";
        }

        var relative = logoPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRootPath, relative);
        if (!File.Exists(full))
        {
            var initial = string.IsNullOrWhiteSpace(company) ? "?" : WebUtility.HtmlEncode(company[..1].ToUpperInvariant());
            return $"""<span class="vcard-logo-fallback">{initial}</span>""";
        }

        var bytes = File.ReadAllBytes(full);
        var ext = Path.GetExtension(full).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/png"
        };
        return $"""<img src="data:{mime};base64,{Convert.ToBase64String(bytes)}" alt="Logo" />""";
    }
}
