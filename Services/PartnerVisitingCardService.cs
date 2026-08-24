using PuppeteerSharp;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IPartnerVisitingCardService
{
    Task<byte[]> CreateCardImageAsync(ChannelPartner partner);
    Task<string> EnsureCardImageAsync(ChannelPartner partner, bool forceRefresh = false);
    string? GetExistingCardPath(ChannelPartner partner);
}

public class PartnerVisitingCardService : IPartnerVisitingCardService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PartnerVisitingCardService> _logger;
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);
    private static string? _chromiumExecutable;

    public PartnerVisitingCardService(IWebHostEnvironment env, ILogger<PartnerVisitingCardService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public string? GetExistingCardPath(ChannelPartner partner)
    {
        var fileName = $"card-{partner.Id}.png";
        var fullPath = Path.Combine(_env.WebRootPath, "uploads", "partners", "cards", fileName);
        if (!File.Exists(fullPath)) return null;
        return $"/uploads/partners/cards/{fileName}";
    }

    public async Task<byte[]> CreateCardImageAsync(ChannelPartner partner)
    {
        var cssPath = Path.Combine(_env.WebRootPath, "css", "visiting-card.css");
        if (!File.Exists(cssPath))
            throw new FileNotFoundException("Visiting card CSS not found.", cssPath);

        var html = PartnerVisitingCardHtmlBuilder.Build(partner, _env.WebRootPath, cssPath);

        var tempDir = Path.Combine(Path.GetTempPath(), "softflip-vcards");
        Directory.CreateDirectory(tempDir);
        var htmlPath = Path.Combine(tempDir, $"card-{partner.Id}-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(htmlPath, html);

        try
        {
            await BrowserLock.WaitAsync();
            try
            {
                var executable = await EnsureChromiumAsync();

                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executable,
                    Args =
                    [
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    ]
                });

                await using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1050,
                    Height = 600,
                    DeviceScaleFactor = 1
                });

                var fileUri = new Uri(htmlPath).AbsoluteUri;
                await page.GoToAsync(fileUri, new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Load, WaitUntilNavigation.DOMContentLoaded],
                    Timeout = 30000
                });

                return await page.ScreenshotDataAsync(new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = false
                });
            }
            finally
            {
                BrowserLock.Release();
            }
        }
        finally
        {
            try { File.Delete(htmlPath); } catch { /* ignore */ }
        }
    }

    public async Task<string> EnsureCardImageAsync(ChannelPartner partner, bool forceRefresh = false)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners", "cards");
        Directory.CreateDirectory(dir);
        var fileName = $"card-{partner.Id}.png";
        var fullPath = Path.Combine(dir, fileName);
        var webPath = $"/uploads/partners/cards/{fileName}";

        if (!forceRefresh && File.Exists(fullPath))
            return webPath;

        var png = await CreateCardImageAsync(partner);
        await File.WriteAllBytesAsync(fullPath, png);
        return webPath;
    }

    private async Task<string> EnsureChromiumAsync()
    {
        if (!string.IsNullOrWhiteSpace(_chromiumExecutable) && File.Exists(_chromiumExecutable))
            return _chromiumExecutable;

        var browserFetcher = new BrowserFetcher();
        var installed = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
        if (installed != null && File.Exists(installed.GetExecutablePath()))
        {
            _chromiumExecutable = installed.GetExecutablePath();
            return _chromiumExecutable;
        }

        _logger.LogInformation("Downloading Chromium for visiting-card PNG generation…");
        var revisionInfo = await browserFetcher.DownloadAsync();
        _chromiumExecutable = revisionInfo.GetExecutablePath();
        return _chromiumExecutable;
    }
}
