using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class Proposal
{
    [Key]
    public int Id { get; set; }

    /// <summary>Enquiry | ClientLead | DemoRequest</summary>
    [Required]
    [StringLength(20)]
    public string LeadType { get; set; } = string.Empty;

    public int LeadId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Scope { get; set; } = string.Empty;

    [StringLength(40)]
    public string TemplateKey { get; set; } = "classic";

    [StringLength(260)]
    public string? FilePath { get; set; }

    public int? ServiceCatalogId { get; set; }
    public ServiceCatalog? Service { get; set; }

    /// <summary>JSON snapshot of selected modules/sub-modules at proposal time.</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SelectedModulesJson { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Partner-quoted new price before discount (null for admin proposals).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalAmount { get; set; }

    /// <summary>Optional discount percent applied on OriginalAmount.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercent { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Revision number within a proposal family (1 = original).</summary>
    public int Version { get; set; } = 1;

    public int? ParentProposalId { get; set; }
    public Proposal? ParentProposal { get; set; }

    public Invoice? Invoice { get; set; }
}
