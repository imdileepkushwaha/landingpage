using System.Net;
using System.Text;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public static class PartnerCertificateHtmlBuilder
{
    public static string Build(ChannelPartner partner, string webRootPath, string cssPath)
    {
        var owner = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(partner.OwnerName) ? partner.CompanyName : partner.OwnerName);
        var mobileRaw = (partner.Mobile ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(mobileRaw) && !mobileRaw.StartsWith("+", StringComparison.Ordinal))
            mobileRaw = "+91 " + mobileRaw.TrimStart('0');
        var mobile = WebUtility.HtmlEncode(mobileRaw);
        var initial = string.IsNullOrWhiteSpace(owner) ? "?" : owner.Trim()[0].ToString().ToUpperInvariant();

        var css = File.ReadAllText(cssPath);
        var softflipLogo = ToDataUri(Path.Combine(webRootPath, "admin", "img", "softflip-logo.png"));
        var photoHtml = BuildPhotoHtml(partner.PhotoPath, initial, webRootPath);

        // Inline SVGs — no CDN/emoji (reliable in Puppeteer PNG/PDF)
        var solutions = new (string Svg, string Label, string Tone)[]
        {
            (SvgCode, "Software Development", "blue"),
            (SvgGlobe, "Website Development", "green"),
            (SvgMortorboard, "School ERP", "blue"),
            (SvgPeople, "HRMS & Payroll", "green"),
            (SvgReceipt, "Billing Software", "green"),
            (SvgBriefcase, "CRM Solutions", "blue"),
            (SvgDiagram, "MLM Software", "green"),
            (SvgLaptop, "Custom Software", "blue")
        };

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        sb.Append("<style>").Append(css);
        sb.Append("@page{size:A4 portrait;margin:0}html,body{width:794px;height:1123px;overflow:hidden}");
        sb.Append(".pcert-sol-ico svg,.pcert-contact-ico svg,.pcert-mobile-ico svg,.pcert-footer svg{display:block;width:1em;height:1em;fill:currentColor}");
        sb.Append("</style></head>");
        sb.Append("<body style=\"margin:0;padding:0;background:#fff;width:794px;height:1123px;overflow:hidden;\">");
        sb.Append("<div class=\"pcert-canvas\"><div class=\"pcert-waves\" aria-hidden=\"true\">");
        sb.Append("<svg class=\"pcert-wave-svg\" width=\"200\" height=\"1123\" viewBox=\"0 0 220 1123\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"none\">");
        sb.Append("<path fill=\"#b8e8f8\" d=\"M0 0 H95 C145 90 40 180 110 280 C175 375 55 470 120 580 C180 680 50 780 105 880 C155 970 70 1040 95 1123 H0 Z\" />");
        sb.Append("<path fill=\"#5bc8ef\" d=\"M0 0 H62 C115 110 25 210 85 320 C150 430 35 530 90 640 C145 750 30 850 78 960 C115 1040 50 1085 70 1123 H0 Z\" />");
        sb.Append("<path fill=\"#0b5ea8\" d=\"M0 0 H38 C78 130 10 240 55 360 C105 490 15 600 58 730 C100 860 18 970 48 1123 H0 Z\" />");
        sb.Append("<path fill=\"#00aeef\" fill-opacity=\"0.65\" d=\"M0 420 C55 480 20 560 48 640 C78 720 15 800 40 880 C60 940 20 1000 0 1040 Z\" />");
        sb.Append("</svg></div><div class=\"pcert-dots\"></div><div class=\"pcert-inner\">");

        sb.Append("<div class=\"pcert-brand\">");
        if (!string.IsNullOrEmpty(softflipLogo))
            sb.Append("<img class=\"pcert-brand-logo\" src=\"").Append(softflipLogo).Append("\" alt=\"Softflip\" />");
        sb.Append("<p class=\"pcert-brand-name\"><span class=\"blue\">SOFTFLIP</span> <span class=\"green\">SOLUTIONS</span></p>");
        sb.Append("<p class=\"pcert-tagline\">— Smart Solutions, Better Future —</p></div>");

        sb.Append("<div class=\"pcert-authorised\">AUTHORISED</div>");
        sb.Append("<div class=\"pcert-title\"><h1>TECHNOLOGY</h1><h2>SUPPORT PARTNER</h2></div>");

        sb.Append("<div class=\"pcert-photo-wrap\"><div class=\"pcert-photo\">").Append(photoHtml).Append("</div>");
        sb.Append("<div class=\"pcert-badge\"><div class=\"pcert-badge-check\">✓</div><strong>TRUSTED<br>PARTNER</strong>");
        sb.Append("<div class=\"stars\"><span>★</span><span>★</span><span>★</span></div></div></div>");

        sb.Append("<div class=\"pcert-name-label\">Partner Name</div>");
        sb.Append("<div class=\"pcert-name-bar\">").Append(owner).Append("</div>");

        sb.Append("<div class=\"pcert-mobile\"><div class=\"pcert-mobile-ico\">").Append(SvgPhone).Append("</div><div class=\"pcert-mobile-copy\">");
        sb.Append("<small>Mobile Number</small><strong>").Append(mobile).Append("</strong></div></div>");

        sb.Append("<div class=\"pcert-solutions-banner\">OUR SOLUTIONS</div><div class=\"pcert-solutions\">");
        foreach (var s in solutions)
        {
            sb.Append("<div class=\"pcert-sol\"><span class=\"pcert-sol-ico ").Append(s.Tone).Append("\">")
              .Append(s.Svg).Append("</span><span class=\"pcert-sol-label\">").Append(s.Label).Append("</span></div>");
        }
        sb.Append("</div>");

        sb.Append("<div class=\"pcert-rating\"><div class=\"pcert-rating-left\">");
        sb.Append("<div class=\"pcert-rating-label\">Client Rating</div>");
        sb.Append("<div class=\"pcert-rating-stars\"><span class=\"on\">★</span><span class=\"on\">★</span><span class=\"on\">★</span><span class=\"on\">★</span><span class=\"half\">★</span></div>");
        sb.Append("</div><div class=\"pcert-rating-score\"><strong>4.5 <em>/ 5</em></strong><span>Based on Client Feedback</span></div></div>");

        sb.Append("<div class=\"pcert-bottom\"><div class=\"pcert-contacts\">");
        sb.Append("<div class=\"pcert-contact\"><span class=\"pcert-contact-ico blue\">").Append(SvgGlobe).Append("</span>");
        sb.Append("<div class=\"pcert-contact-copy\"><small>Visit Our Website</small><strong>www.softflipsolutions.com</strong></div></div>");
        sb.Append("<div class=\"pcert-contact\"><span class=\"pcert-contact-ico green\">").Append(SvgEnvelope).Append("</span>");
        sb.Append("<div class=\"pcert-contact-copy\"><small>Email Us</small><strong>info@softflipsolutions.com</strong></div></div>");
        sb.Append("</div>");

        sb.Append("<div class=\"pcert-footer\">");
        sb.Append("<span>").Append(SvgCheck).Append(" Reliable Support</span><span class=\"pcert-footer-dot\"></span>");
        sb.Append("<span>").Append(SvgBulb).Append(" Innovative Solutions</span><span class=\"pcert-footer-dot\"></span>");
        sb.Append("<span>").Append(SvgGraph).Append(" Your Growth, Our Commitment</span></div></div>");
        sb.Append("</div></div></body></html>");
        return sb.ToString();
    }

    private static string Svg(string path) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\" aria-hidden=\"true\">{path}</svg>";

    private static string SvgCode => Svg("<path d=\"M5.854 4.854a.5.5 0 1 0-.708-.708l-3.5 3.5a.5.5 0 0 0 0 .708l3.5 3.5a.5.5 0 0 0 .708-.708L2.707 8zm4.292 0a.5.5 0 0 1 .708-.708l3.5 3.5a.5.5 0 0 1 0 .708l-3.5 3.5a.5.5 0 0 1-.708-.708L13.293 8z\"/>");
    private static string SvgGlobe => Svg("<path d=\"M0 8a8 8 0 1 1 16 0A8 8 0 0 1 0 8m7.5-6.923c-.67.204-1.335.82-1.887 1.855A8 8 0 0 0 5.145 4H7.5zM4.09 4a9.3 9.3 0 0 1 .64-1.539 7 7 0 0 1 .45-.688A7.03 7.03 0 0 0 2.076 4zm0 1H2.076A7 7 0 0 0 2 8c0 .71.1 1.395.284 2.043l1.903-.48A18 18 0 0 1 4.09 5m.055 4.5a17 17 0 0 1-.166-2.5H2.076a7 7 0 0 0 1.163 3.5zm1.1 1.5c.46.826 1.023 1.414 1.603 1.723V9.5H5.145a15 15 0 0 0 .1 1.5M8.5 14.923c.67-.204 1.335-.82 1.887-1.855A8 8 0 0 0 10.855 12H8.5zm3.41-1a9.3 9.3 0 0 1-.64 1.539 7 7 0 0 1-.45.688A7.03 7.03 0 0 0 13.924 12zm.055-1.5c.05-.5.08-1.01.09-1.5h1.903A7 7 0 0 0 14 8c0-.71-.1-1.395-.284-2.043l-1.903.48c.04.5.07 1.01.09 1.563M10.855 4a15 15 0 0 0-.1-1.5c.46-.826 1.023-1.414 1.603-1.723V4.5zM8.5 1.077V4.5h2.355a8 8 0 0 0-1.887-1.855A7 7 0 0 0 8.5 1.077\"/>");
    private static string SvgMortorboard => Svg("<path d=\"M8.211 2.047a.5.5 0 0 0-.422 0l-7.5 3.5a.5.5 0 0 0 .025.917l7.5 3a.5.5 0 0 0 .372 0L14 7.14V13a1 1 0 0 0-1 1v2h3v-2a1 1 0 0 0-1-1V6.739l.686-.275a.5.5 0 0 0 .025-.917zM8 8.46 1.758 5.965 8 3.052l6.242 2.913z\"/>");
    private static string SvgPeople => Svg("<path d=\"M7 14s-1 0-1-1 1-4 5-4 5 3 5 4-1 1-1 1zm4-6a3 3 0 1 0 0-6 3 3 0 0 0 0 6m-5.784 6A2.24 2.24 0 0 1 5 13c0-1.355.68-2.75 1.936-3.72A6.4 6.4 0 0 0 5 9c-4 0-5 3-5 4s1 1 1 1zM4.5 8a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5\"/>");
    private static string SvgReceipt => Svg("<path d=\"M1.92.506a.5.5 0 0 1 .434.14L3 1.293l.646-.647a.5.5 0 0 1 .708 0L5 1.293l.646-.647a.5.5 0 0 1 .708 0L7 1.293l.646-.647a.5.5 0 0 1 .708 0L9 1.293l.646-.647a.5.5 0 0 1 .708 0L11 1.293l.646-.647a.5.5 0 0 1 .708 0L13 1.293l.646-.647a.5.5 0 0 1 .708 0L15 1.293V15.5a.5.5 0 0 1-.5.5h-13a.5.5 0 0 1-.5-.5V1.293L1.92.506zM3 3.5v1h10v-1zm0 2v1h6v-1zm0 2v1h6v-1zm0 2v1h6v-1zm8-4v1h2v-1zm0 2v1h2v-1zm0 2v1h2v-1z\"/>");
    private static string SvgBriefcase => Svg("<path d=\"M6.5 1A1.5 1.5 0 0 0 5 2.5V3H1.5A1.5 1.5 0 0 0 0 4.5v8A1.5 1.5 0 0 0 1.5 14h13a1.5 1.5 0 0 0 1.5-1.5v-8A1.5 1.5 0 0 0 14.5 3H11v-.5A1.5 1.5 0 0 0 9.5 1zm0 1h3a.5.5 0 0 1 .5.5V3H6v-.5a.5.5 0 0 1 .5-.5M1.5 4h13a.5.5 0 0 1 .5.5v8a.5.5 0 0 1-.5.5h-13a.5.5 0 0 1-.5-.5v-8a.5.5 0 0 1 .5-.5\"/>");
    private static string SvgDiagram => Svg("<path d=\"M6 3.5A1.5 1.5 0 0 1 7.5 2h1A1.5 1.5 0 0 1 10 3.5v1A1.5 1.5 0 0 1 8.5 6v1H11a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-1 0V8h-5v.5a.5.5 0 0 1-1 0v-1A.5.5 0 0 1 5 7h2.5V6A1.5 1.5 0 0 1 6 4.5zM8.5 5a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5zM3 11.5A1.5 1.5 0 0 1 4.5 10h1A1.5 1.5 0 0 1 7 11.5v1A1.5 1.5 0 0 1 5.5 14h-1A1.5 1.5 0 0 1 3 12.5zm1.5-.5a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5zm4.5.5A1.5 1.5 0 0 1 10.5 10h1a1.5 1.5 0 0 1 1.5 1.5v1a1.5 1.5 0 0 1-1.5 1.5h-1A1.5 1.5 0 0 1 9 12.5zm1.5-.5a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5z\"/>");
    private static string SvgLaptop => Svg("<path d=\"M13.5 3a.5.5 0 0 1 .5.5V11H2V3.5a.5.5 0 0 1 .5-.5zm-11-1A1.5 1.5 0 0 0 1 3.5V12h14V3.5A1.5 1.5 0 0 0 13.5 2zM0 12.5h16a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 12.5\"/>");
    private static string SvgPhone => Svg("<path d=\"M3.654 1.328a.678.678 0 0 0-1.015-.063L1.605 2.3c-.483.484-.661 1.169-.45 1.77a17.6 17.6 0 0 0 4.168 6.608 17.6 17.6 0 0 0 6.608 4.168c.601.211 1.286.033 1.77-.45l1.034-1.034a.678.678 0 0 0-.063-1.015l-2.307-1.794a.68.68 0 0 0-.58-.122l-2.248.547a1.75 1.75 0 0 1-1.71-.739L5.715 6.05a1.75 1.75 0 0 1-.739-1.71l.547-2.248a.68.68 0 0 0-.122-.58zM1.884.511a1.745 1.745 0 0 1 2.612.163L6.29 2.98c.329.423.445.967.315 1.49l-.547 2.19a.68.68 0 0 0 .178.643l2.457 2.457a.68.68 0 0 0 .644.178l2.189-.547a1.75 1.75 0 0 1 1.49.315l2.306 1.794c.829.645.905 1.87.163 2.611l-1.034 1.034c-.74.74-1.846 1.065-2.877.702a18.6 18.6 0 0 1-7.01-4.42 18.6 18.6 0 0 1-4.42-7.009c-.362-1.03-.037-2.137.703-2.877z\"/>");
    private static string SvgEnvelope => Svg("<path d=\"M.05 3.555A2 2 0 0 1 2 2h12a2 2 0 0 1 1.95 1.555L8 8.414zM0 4.697v7.104l5.803-3.558zM6.761 8.83l-6.57 4.027A2 2 0 0 0 2 14h12a2 2 0 0 0 1.808-1.144l-6.57-4.027L8 9.586zm3.436-.586L16 11.801V4.697z\"/>");
    private static string SvgCheck => Svg("<path d=\"M10.97 4.97a.75.75 0 0 1 1.07 1.05l-3.99 4.99a.75.75 0 0 1-1.08.02L4.324 8.384a.75.75 0 1 1 1.06-1.06l2.094 2.093 3.473-4.425z\"/>");
    private static string SvgBulb => Svg("<path d=\"M2 6a6 6 0 1 1 10.174 4.31c-.203.196-.359.4-.453.619l-.762 1.769A.5.5 0 0 1 10.5 13h-5a.5.5 0 0 1-.46-.302l-.761-1.77a2 2 0 0 0-.453-.618A5.98 5.98 0 0 1 2 6m6-5a5 5 0 0 0-3.479 8.592c.10.20.0.3.514.826l.75 1.743h4.43l.75-1.743c.079-.183.255-.459.514-.825A5 5 0 0 0 8 1\"/>");
    private static string SvgGraph => Svg("<path fill-rule=\"evenodd\" d=\"M0 0h1v15h15v1H0zm14.817 3.113a.5.5 0 0 1 .07.704l-4.5 5.5a.5.5 0 0 1-.74.037L7.06 6.767l-3.656 5.027a.5.5 0 0 1-.808-.588l4-5.5a.5.5 0 0 1 .758-.06l2.609 2.61 4.15-5.073a.5.5 0 0 1 .704-.07\"/>");

    private static string BuildPhotoHtml(string? logoPath, string initial, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return WebUtility.HtmlEncode(initial);

        var relative = logoPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRootPath, relative);
        var data = ToDataUri(full);
        if (string.IsNullOrEmpty(data))
            return WebUtility.HtmlEncode(initial);

        return $"<img src=\"{data}\" alt=\"Partner\" />";
    }

    private static string ToDataUri(string fullPath)
    {
        if (!File.Exists(fullPath)) return "";
        var bytes = File.ReadAllBytes(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/png"
        };
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
