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
    public string? LogoPath { get; set; }

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
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PartnerClient> Clients { get; set; } = new();
    public List<PartnerProposal> Proposals { get; set; } = new();

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
        IsAuthorizedPartner = true
    };
}
