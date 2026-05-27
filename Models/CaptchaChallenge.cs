namespace SoftflipSolutions.Models;

public class CaptchaChallenge
{
    public string Token { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
}

public class CaptchaFieldViewModel
{
    public string Prefix { get; set; } = string.Empty;
    public CaptchaChallenge Challenge { get; set; } = new();
}
