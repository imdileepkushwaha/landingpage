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

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PartnerProposal> Proposals { get; set; } = new();
}
