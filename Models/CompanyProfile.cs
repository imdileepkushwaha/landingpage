namespace SoftflipSolutions.Models;

public class CompanyProfile
{
    public string CompanyName { get; set; } = "Softflip Solutions";
    public string Tagline { get; set; } = "Software & Digital Solutions";
    public string Address { get; set; } = string.Empty;
    public string Gstin { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
    public string ContactWhatsApp { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;

    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = "Authorized Signatory";
    public string SignaturePath { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIfsc { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string UpiId { get; set; } = string.Empty;
    public string UpiName { get; set; } = string.Empty;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoPath);
    public bool HasSignature => !string.IsNullOrWhiteSpace(SignaturePath);
    public bool HasBankDetails =>
        !string.IsNullOrWhiteSpace(BankName) ||
        !string.IsNullOrWhiteSpace(BankAccountNumber) ||
        !string.IsNullOrWhiteSpace(BankIfsc) ||
        !string.IsNullOrWhiteSpace(UpiId);
}
