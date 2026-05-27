using Microsoft.Extensions.Caching.Memory;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface ICaptchaService
{
    CaptchaChallenge GenerateChallenge();
    bool Validate(string token, string userAnswer);
}

public class CaptchaService : ICaptchaService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CaptchaLifetime = TimeSpan.FromMinutes(10);

    public CaptchaService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CaptchaChallenge GenerateChallenge()
    {
        var first = Random.Shared.Next(2, 10);
        var second = Random.Shared.Next(1, 10);
        var token = Guid.NewGuid().ToString("N");

        _cache.Set(GetCacheKey(token), (first + second).ToString(), CaptchaLifetime);

        return new CaptchaChallenge
        {
            Token = token,
            Question = $"What is {first} + {second}?"
        };
    }

    public bool Validate(string token, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userAnswer))
        {
            return false;
        }

        var key = GetCacheKey(token);
        if (!_cache.TryGetValue(key, out string? expected) || expected is null)
        {
            return false;
        }

        _cache.Remove(key);

        return int.TryParse(userAnswer.Trim(), out var answer)
            && int.TryParse(expected, out var expectedAnswer)
            && answer == expectedAnswer;
    }

    private static string GetCacheKey(string token) => $"captcha:{token}";
}
