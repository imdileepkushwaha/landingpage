using PuppeteerSharp;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IPartnerCertificateService
{
    Task<byte[]> CreateCertificateImageAsync(ChannelPartner partner);
    Task<byte[]> CreateCertificatePdfAsync(ChannelPartner partner);
    Task<string> EnsureCertificateImageAsync(ChannelPartner partner, bool forceRefresh = false);
    string? GetExistingCertificatePath(ChannelPartner partner);
}

public class PartnerCertificateService : IPartnerCertificateService
{
    public const int CanvasWidth = 794;
    public const int CanvasHeight = 1123;

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PartnerCertificateService> _logger;
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);
    private static string? _chromiumExecutable;

    public PartnerCertificateService(IWebHostEnvironment env, ILogger<PartnerCertificateService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public string? GetExistingCertificatePath(ChannelPartner partner)
    {
        var fileName = $"cert-{partner.Id}.png";
        var fullPath = Path.Combine(_env.WebRootPath, "uploads", "partners", "certificates", fileName);
        if (!File.Exists(fullPath)) return null;
        return $"/uploads/partners/certificates/{fileName}";
    }

    public async Task<byte[]> CreateCertificateImageAsync(ChannelPartner partner)
    {
        return await RenderAsync(partner, asPdf: false);
    }

    public async Task<byte[]> CreateCertificatePdfAsync(ChannelPartner partner)
    {
        return await RenderAsync(partner, asPdf: true);
    }

    public async Task<string> EnsureCertificateImageAsync(ChannelPartner partner, bool forceRefresh = false)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "partners", "certificates");
        Directory.CreateDirectory(dir);
        var fileName = $"cert-{partner.Id}.png";
        var fullPath = Path.Combine(dir, fileName);
        var webPath = $"/uploads/partners/certificates/{fileName}";

        if (!forceRefresh && File.Exists(fullPath))
            return webPath;

        var png = await CreateCertificateImageAsync(partner);
        await File.WriteAllBytesAsync(fullPath, png);
        return webPath;
    }

    private async Task<byte[]> RenderAsync(ChannelPartner partner, bool asPdf)
    {
        var cssPath = Path.Combine(_env.WebRootPath, "css", "partner-certificate.css");
        if (!File.Exists(cssPath))
            throw new FileNotFoundException("Partner certificate CSS not found.", cssPath);

        var html = PartnerCertificateHtmlBuilder.Build(partner, _env.WebRootPath, cssPath);
        var tempDir = Path.Combine(Path.GetTempPath(), "softflip-certs");
        Directory.CreateDirectory(tempDir);
        var htmlPath = Path.Combine(tempDir, $"cert-{partner.Id}-{Guid.NewGuid():N}.html");
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
                    Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"]
                });

                await using var page = await browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = CanvasWidth,
                    Height = CanvasHeight,
                    DeviceScaleFactor = 2
                });

                await page.GoToAsync(new Uri(htmlPath).AbsoluteUri, new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Load, WaitUntilNavigation.Networkidle0],
                    Timeout = 45000
                });
                await page.EvaluateExpressionAsync("document.fonts && document.fonts.ready ? document.fonts.ready : Promise.resolve()");
                await Task.Delay(200);

                if (asPdf)
                {
                    return await page.PdfDataAsync(new PdfOptions
                    {
                        Width = "210mm",
                        Height = "297mm",
                        PrintBackground = true,
                        PreferCSSPageSize = false
                    });
                }

                return await page.ScreenshotDataAsync(new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = false,
                    FullPage = false
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

        _logger.LogInformation("Downloading Chromium for partner certificate…");
        var revisionInfo = await browserFetcher.DownloadAsync();
        _chromiumExecutable = revisionInfo.GetExecutablePath();
        return _chromiumExecutable;
    }
}
