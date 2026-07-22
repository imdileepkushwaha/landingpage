using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using Microsoft.EntityFrameworkCore;

namespace SoftflipSolutions.Services;

public interface ICompanyProfileService
{
    Task<CompanyProfile> GetAsync();
    Task SaveCompanyAsync(CompanyProfile profile);
    Task SaveContactsAsync(CompanyProfile profile);
    Task SaveSignatureAsync(CompanyProfile profile);
    Task<string?> SaveUploadAsync(IFormFile file, string folder, string prefix);
}

public class CompanyProfileService : ICompanyProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public CompanyProfileService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<CompanyProfile> GetAsync()
    {
        var dict = await _context.AdminSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        string G(string key, string fallback = "") =>
            dict.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

        return new CompanyProfile
        {
            CompanyName = G("CompanyName", "Softflip Solutions"),
            Tagline = G("CompanyTagline", "Software & Digital Solutions"),
            Address = G("CompanyAddress"),
            Gstin = G("CompanyGstin"),
            Website = G("CompanyWebsite"),
            LogoPath = G("CompanyLogoPath"),
            ContactPhone = G("ContactPhone"),
            ContactWhatsApp = G("ContactWhatsApp"),
            ContactEmail = G("ContactEmail"),
            ContactPerson = G("ContactPerson"),
            SignatoryName = G("SignatoryName"),
            SignatoryTitle = G("SignatoryTitle", "Authorized Signatory"),
            SignaturePath = G("SignaturePath"),
            BankName = G("BankName"),
            BankAccountName = G("BankAccountName"),
            BankAccountNumber = G("BankAccountNumber"),
            BankIfsc = G("BankIfsc"),
            BankBranch = G("BankBranch"),
            UpiId = G("UpiId"),
            UpiName = G("UpiName")
        };
    }

    public Task SaveCompanyAsync(CompanyProfile profile) => UpsertAsync(new Dictionary<string, string>
    {
        ["CompanyName"] = profile.CompanyName?.Trim() ?? "",
        ["CompanyTagline"] = profile.Tagline?.Trim() ?? "",
        ["CompanyAddress"] = profile.Address?.Trim() ?? "",
        ["CompanyGstin"] = profile.Gstin?.Trim() ?? "",
        ["CompanyWebsite"] = profile.Website?.Trim() ?? "",
        ["CompanyLogoPath"] = profile.LogoPath?.Trim() ?? "",
        ["BankName"] = profile.BankName?.Trim() ?? "",
        ["BankAccountName"] = profile.BankAccountName?.Trim() ?? "",
        ["BankAccountNumber"] = profile.BankAccountNumber?.Trim() ?? "",
        ["BankIfsc"] = profile.BankIfsc?.Trim() ?? "",
        ["BankBranch"] = profile.BankBranch?.Trim() ?? "",
        ["UpiId"] = profile.UpiId?.Trim() ?? "",
        ["UpiName"] = profile.UpiName?.Trim() ?? ""
    });

    public Task SaveContactsAsync(CompanyProfile profile) => UpsertAsync(new Dictionary<string, string>
    {
        ["ContactPhone"] = profile.ContactPhone?.Trim() ?? "",
        ["ContactWhatsApp"] = profile.ContactWhatsApp?.Trim() ?? "",
        ["ContactEmail"] = profile.ContactEmail?.Trim() ?? "",
        ["ContactPerson"] = profile.ContactPerson?.Trim() ?? ""
    });

    public Task SaveSignatureAsync(CompanyProfile profile) => UpsertAsync(new Dictionary<string, string>
    {
        ["SignatoryName"] = profile.SignatoryName?.Trim() ?? "",
        ["SignatoryTitle"] = profile.SignatoryTitle?.Trim() ?? "Authorized Signatory",
        ["SignaturePath"] = profile.SignaturePath?.Trim() ?? ""
    });

    public async Task<string?> SaveUploadAsync(IFormFile file, string folder, string prefix)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > 2 * 1024 * 1024) throw new InvalidOperationException("File must be under 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new InvalidOperationException("Only PNG, JPG, or WEBP images are allowed.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var fileName = $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"/uploads/{folder}/{fileName}";
    }

    private async Task UpsertAsync(Dictionary<string, string> values)
    {
        foreach (var kvp in values)
        {
            var setting = await _context.AdminSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
            if (setting == null)
                _context.AdminSettings.Add(new AdminSetting { Key = kvp.Key, Value = kvp.Value });
            else
                setting.Value = kvp.Value;
        }
        await _context.SaveChangesAsync();
    }
}
