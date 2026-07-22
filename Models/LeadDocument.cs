using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class LeadDocument
{
    [Key]
    public int Id { get; set; }

    /// <summary>Enquiry | ClientLead | DemoRequest</summary>
    [Required]
    [StringLength(20)]
    public string LeadType { get; set; } = string.Empty;

    public int LeadId { get; set; }

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = "Project";

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
