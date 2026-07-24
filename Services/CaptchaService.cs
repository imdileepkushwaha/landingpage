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
        var token = Guid.NewGuid().ToString("N");
        string question;
        int expected;

        // Mix operations so simple bots cannot always assume addition.
        switch (Random.Shared.Next(0, 3))
        {
            case 0:
            {
                var first = Random.Shared.Next(3, 12);
                var second = Random.Shared.Next(1, 9);
                expected = first + second;
                question = $"What is {first} + {second}?";
                break;
            }
            case 1:
            {
                var first = Random.Shared.Next(8, 18);
                var second = Random.Shared.Next(1, 7);
                expected = first - second;
                question = $"What is {first} − {second}?";
                break;
            }
            default:
            {
                var first = Random.Shared.Next(2, 9);
                var second = Random.Shared.Next(2, 6);
                expected = first * second;
                question = $"What is {first} × {second}?";
                break;
            }
        }

        _cache.Set(GetCacheKey(token), expected.ToString(), CaptchaLifetime);

        return new CaptchaChallenge
        {
            Token = token,
            Question = question
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
