using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class PartnerProposal
{
    [Key]
    public int Id { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }

    public int PartnerClientId { get; set; }
    public PartnerClient? PartnerClient { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Scope { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Partner-entered new price before discount.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalAmount { get; set; }

    /// <summary>Optional discount % on OriginalAmount.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercent { get; set; }

    public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(15);

    [StringLength(40)]
    public string TemplateKey { get; set; } = "classic";

    [StringLength(300)]
    public string? FilePath { get; set; }

    public int? ServiceCatalogId { get; set; }
    public ServiceCatalog? Service { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? SelectedModulesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Admin-tracked partner commission payout status.</summary>
    public bool IsCommissionPaid { get; set; }

    public DateTime? CommissionPaidAt { get; set; }
}
