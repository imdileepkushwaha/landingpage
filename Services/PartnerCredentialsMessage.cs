namespace SoftflipSolutions.Services;

/// <summary>Welcome + login credentials copy for partner email / WhatsApp.</summary>
public static class PartnerCredentialsMessage
{
    // Build from code points so source-file encoding can never corrupt emojis.
    public static string PartyPop => char.ConvertFromUtf32(0x1F389);       // 🎉
    public static string HandshakeEmoji => char.ConvertFromUtf32(0x1F91D); // 🤝
    public static string LinkEmoji => char.ConvertFromUtf32(0x1F517);      // 🔗
    public static string PersonEmoji => char.ConvertFromUtf32(0x1F464);    // 👤
    public static string LockEmoji => char.ConvertFromUtf32(0x1F510);      // 🔐
    public static string RocketEmoji => char.ConvertFromUtf32(0x1F680);    // 🚀

    /// <summary>HTML numeric entities — safe in email even if transfer charset is wrong.</summary>
    public static string PartyPopHtml => "&#127881;";
    public static string HandshakeHtml => "&#129309;";
    public static string LinkHtml => "&#128279;";
    public static string PersonHtml => "&#128100;";
    public static string LockHtml => "&#128272;";
    public static string RocketHtml => "&#128640;";

    public static string BuildPlainText(string ownerName, string loginUrl, string loginId, string password)
    {
        return
            "Welcome to Softflip Solutions! " + PartyPop + "\n\n" +
            "Dear Partner, Mr./Ms. " + ownerName + ",\n\n" +
            "We are happy to welcome you as an Authorized Technology Support Partner. " + HandshakeEmoji + "\n\n" +
            "Your Partner Login Credentials are:\n\n" +
            LinkEmoji + " Login URL: " + loginUrl + "\n" +
            PersonEmoji + " Login ID: " + loginId + "\n" +
            LockEmoji + " Password: " + password + "\n\n" +
            "Please keep your login credentials safe and do not share your password with anyone.\n\n" +
            "For any support or assistance, feel free to contact us.\n\n" +
            "Welcome aboard, and we look forward to a successful journey together! " + RocketEmoji + "\n\n" +
            "Regards,\nSoftflip Solutions";
    }

    public static string BuildWhatsAppUrl(string mobileDigitsOrEmpty, string ownerName, string loginUrl, string loginId, string password)
    {
        var text = BuildPlainText(ownerName, loginUrl, loginId, password);
        var encoded = Uri.EscapeDataString(text);
        if (string.IsNullOrWhiteSpace(mobileDigitsOrEmpty))
            return "https://api.whatsapp.com/send?text=" + encoded;
        return "https://api.whatsapp.com/send?phone=" + mobileDigitsOrEmpty + "&text=" + encoded;
    }
}
