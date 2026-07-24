using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace SoftflipSolutions.Services;

public interface IFormSpamGuard
{
    string CreateFormToken();
    /// <summary>
    /// Returns false when the submission should be blocked.
    /// If silentReject is true, show a fake success (bot/honeypot) without saving.
    /// </summary>
    bool TryValidate(string? formToken, string? honeypot, string clientKey, out bool silentReject, out string? errorMessage);
}

public class FormSpamGuardService : IFormSpamGuard
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan TokenTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan MinFillTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(15);
    private const int MaxSubmissionsPerWindow = 4;

    public FormSpamGuardService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateFormToken()
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{nonce}:{issuedAt}";
        _cache.Set(GetTokenKey(nonce), issuedAt, TokenTtl);
        return payload;
    }

    public bool TryValidate(string? formToken, string? honeypot, string clientKey, out bool silentReject, out string? errorMessage)
    {
        silentReject = false;
        errorMessage = null;

        // Bots that fill every field — do not reveal rejection.
        if (!string.IsNullOrWhiteSpace(honeypot))
        {
            silentReject = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(formToken))
        {
            errorMessage = "Security check failed. Please refresh the page and try again.";
            return false;
        }

        var parts = formToken.Split(':', 2);
        if (parts.Length != 2
            || string.IsNullOrWhiteSpace(parts[0])
            || !long.TryParse(parts[1], out var issuedAt))
        {
            errorMessage = "Security check failed. Please refresh the page and try again.";
            return false;
        }

        var cacheKey = GetTokenKey(parts[0]);
        if (!_cache.TryGetValue(cacheKey, out long _))
        {
            errorMessage = "Security check expired. Please refresh the page and try again.";
            return false;
        }

        // One-time use
        _cache.Remove(cacheKey);

        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issuedAt;
        if (age < MinFillTime.TotalSeconds)
        {
            silentReject = true;
            return false;
        }

        if (age > TokenTtl.TotalSeconds)
        {
            errorMessage = "Form expired. Please refresh the page and try again.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(clientKey))
            clientKey = "unknown";

        var rateKey = $"form-rate:{clientKey}";
        var count = _cache.GetOrCreate(rateKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = RateWindow;
            return 0;
        });

        if (count >= MaxSubmissionsPerWindow)
        {
            errorMessage = "Too many submissions from your network. Please try again later.";
            return false;
        }

        _cache.Set(rateKey, count + 1, RateWindow);
        return true;
    }

    private static string GetTokenKey(string nonce) => $"form-token:{nonce}";
}
