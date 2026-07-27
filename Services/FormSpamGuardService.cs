using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace SoftflipSolutions.Services;

public interface IFormSpamGuard
{
    string CreateFormToken();
    bool TryValidate(string? formToken, string clientKey, out string? errorMessage);
}

/// <summary>
/// HMAC-signed form tokens (no server memory required for token check — safe across IIS recycle).
/// Soft IP rate-limit still uses memory cache.
/// </summary>
public class FormSpamGuardService : IFormSpamGuard
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan TokenTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(15);
    private const int MaxSubmissionsPerWindow = 8;

    // App-specific signing key (not a user secret — prevents forged tokens).
    private static readonly byte[] SigningKey = Encoding.UTF8.GetBytes(
        "SoftflipSolutions.FormSpamGuard.v1.2026");

    public FormSpamGuardService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateFormToken()
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = $"{nonce}:{issuedAt}";
        return $"{payload}:{Sign(payload)}";
    }

    public bool TryValidate(string? formToken, string clientKey, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(formToken))
        {
            errorMessage = "Security check failed. Please refresh the page and try again.";
            return false;
        }

        var parts = formToken.Split(':');
        if (parts.Length != 3
            || string.IsNullOrWhiteSpace(parts[0])
            || !long.TryParse(parts[1], out var issuedAt)
            || string.IsNullOrWhiteSpace(parts[2]))
        {
            errorMessage = "Security check failed. Please refresh the page and try again.";
            return false;
        }

        var payload = $"{parts[0]}:{parts[1]}";
        if (!FixedTimeEquals(Sign(payload), parts[2]))
        {
            errorMessage = "Security check failed. Please refresh the page and try again.";
            return false;
        }

        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issuedAt;
        if (age < 0 || age > TokenTtl.TotalSeconds)
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

    private static string Sign(string payload)
    {
        using var hmac = new HMACSHA256(SigningKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
