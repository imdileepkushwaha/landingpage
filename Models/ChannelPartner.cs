using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class ChannelPartner
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Owner Name")]
    public string OwnerName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "GST Number")]
    public string? Gstin { get; set; }

    [Required]
    [StringLength(25)]
    [Display(Name = "Mobile")]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Company Logo")]
    public string? LogoPath { get; set; }

    [StringLength(300)]
    [Display(Name = "Profile Photo")]
    public string? PhotoPath { get; set; }

    [Required]
    [StringLength(400)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(12)]
    public string Pincode { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Required]
    [StringLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Admin-visible partner panel password (for resend). Login still verifies PasswordHash.</summary>
    [StringLength(100)]
    [Display(Name = "Login Password")]
    public string? LoginPassword { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Partner-managed payment details (Edit Profile)
    [StringLength(120)]
    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [StringLength(120)]
    [Display(Name = "Account Holder Name")]
    public string? BankAccountName { get; set; }

    [StringLength(40)]
    [Display(Name = "Account Number")]
    public string? BankAccountNumber { get; set; }

    [StringLength(20)]
    [Display(Name = "IFSC")]
    public string? BankIfsc { get; set; }

    [StringLength(120)]
    [Display(Name = "Branch")]
    public string? BankBranch { get; set; }

    [StringLength(100)]
    [Display(Name = "UPI ID")]
    public string? UpiId { get; set; }

    [StringLength(120)]
    [Display(Name = "UPI Display Name")]
    public string? UpiName { get; set; }

    [StringLength(300)]
    [Display(Name = "UPI QR")]
    public string? UpiQrPath { get; set; }

    /// <summary>Public referral code for share links (?ref=CODE).</summary>
    [StringLength(40)]
    public string? ReferralCode { get; set; }

    public List<PartnerClient> Clients { get; set; } = new();
    public List<PartnerProposal> Proposals { get; set; } = new();
    public List<PartnerInvoice> Invoices { get; set; } = new();
    public List<PartnerTicket> Tickets { get; set; } = new();
    public List<PartnerNotification> Notifications { get; set; } = new();

    [NotMapped]
    public string LocationLabel => $"{City}, {State}";

    [NotMapped]
    public string FullAddress =>
        string.Join(", ", new[] { Address, City, State, Pincode }.Where(s => !string.IsNullOrWhiteSpace(s)));

    [NotMapped]
    public string DisplayWebsite
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Website)) return string.Empty;
            var value = Website.Trim();
            if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                value = value[8..];
            else if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                value = value[7..];
            return value.TrimEnd('/');
        }
    }

    [NotMapped]
    public string? AvatarPath =>
        !string.IsNullOrWhiteSpace(PhotoPath) ? PhotoPath :
        !string.IsNullOrWhiteSpace(LogoPath) ? LogoPath : null;

    [NotMapped]
    public bool HasBankDetails =>
        !string.IsNullOrWhiteSpace(BankName) ||
        !string.IsNullOrWhiteSpace(BankAccountNumber) ||
        !string.IsNullOrWhiteSpace(BankIfsc) ||
        !string.IsNullOrWhiteSpace(UpiId) ||
        !string.IsNullOrWhiteSpace(UpiQrPath);

    public CompanyProfile ToCompanyProfile() => new()
    {
        CompanyName = CompanyName,
        Tagline = "Authorized Channel Partner",
        Address = FullAddress,
        Gstin = Gstin ?? "",
        LogoPath = LogoPath ?? "",
        ContactPhone = Mobile,
        ContactWhatsApp = Mobile,
        ContactEmail = Email,
        Website = Website ?? "",
        ContactPerson = OwnerName,
        SignatoryName = OwnerName,
        SignatoryTitle = "Channel Partner",
        IsAuthorizedPartner = true,
        BankName = BankName ?? "",
        BankAccountName = BankAccountName ?? "",
        BankAccountNumber = BankAccountNumber ?? "",
        BankIfsc = BankIfsc ?? "",
        BankBranch = BankBranch ?? "",
        UpiId = UpiId ?? "",
        UpiName = UpiName ?? ""
    };
}
