using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class PartnerNotification
{
    [Key]
    public int Id { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Info | Warning | Success</summary>
    [StringLength(20)]
    public string Type { get; set; } = "Info";

    [StringLength(300)]
    public string? Url { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
