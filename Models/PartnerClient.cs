using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class PartnerClient
{
    [Key]
    public int Id { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Client Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(25)]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(25)]
    [Display(Name = "WhatsApp Number")]
    public string? WhatsApp { get; set; }

    [Required]
    [StringLength(500)]
    public string Requirement { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Budget { get; set; }

    /// <summary>Pipeline stage: New | Quotation | Demo | Negotiation | Won | Lost</summary>
    [Required]
    [StringLength(30)]
    public string Stage { get; set; } = PartnerClientStages.New;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Softflip CRM lead type this client was assigned from (Enquiry / ClientLead / DemoRequest).</summary>
    [StringLength(30)]
    public string? SourceLeadType { get; set; }

    public int? SourceLeadId { get; set; }

    public DateTime? AssignedAt { get; set; }

    [StringLength(100)]
    public string? AssignedBy { get; set; }

    [StringLength(500)]
    public string? AssignNote { get; set; }

    public bool IsAssignedFromAdmin =>
        !string.IsNullOrWhiteSpace(SourceLeadType) && SourceLeadId.HasValue;

    public List<PartnerProposal> Proposals { get; set; } = new();
}
