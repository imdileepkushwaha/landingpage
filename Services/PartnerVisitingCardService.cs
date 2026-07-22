using PuppeteerSharp;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IPartnerVisitingCardService
{
    Task<byte[]> CreateCardImageAsync(ChannelPartner partner);
    Task<string> EnsureCardImageAsync(ChannelPartner partner);
}

public class PartnerVisitingCardService : IPartnerVisitingCardService
{
    private readonly IWebHostEnvironment _env;
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);

    public PartnerVisitingCardService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<byte[]> CreateCardImageAsync(ChannelPartner partner)
    {
        var cssPath = Path.Combine(_env.WebRootPath, "css", "visiting-card.css");
        var html = PartnerVisitingCardHtmlBuilder.Build(partner, _env.WebRootPath, cssPath);

        var tempDir = Path.Combine(Path.GetTempPath(), "softflip-vcards");
        Directory.CreateDirectory(tempDir);
        var htmlPath = Path.Combine(tempDir, $"card-{partner.Id}-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(htmlPath, html);

        try
        {
            await BrowserLock.WaitAsync();
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox"]
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
                WaitUntil = [WaitUntilNavigation.Load, WaitUntilNavigation.DOMContentLoaded]
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
            try { File.Delete(htmlPath); } catch { /* ignore */ }
        }
    }

    public async Task<string> EnsureCardImageAsync(ChannelPartner partner)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners", "cards");
        Directory.CreateDirectory(dir);
        var fileName = $"card-{partner.Id}.png";
        var fullPath = Path.Combine(dir, fileName);
        var webPath = $"/uploads/partners/cards/{fileName}";

        var png = await CreateCardImageAsync(partner);
        await File.WriteAllBytesAsync(fullPath, png);
        return webPath;
    }
}
