using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class PartnerTicket
{
    [Key]
    public int Id { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Open | InProgress | Resolved | Closed</summary>
    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Open";

    [StringLength(2000)]
    public string? AdminReply { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
