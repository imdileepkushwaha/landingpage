using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class EmployeeDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    public int? TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public EmployeeDocumentTemplate? Template { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string DocumentType { get; set; } = "Custom";

    [Required]
    [StringLength(300)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ContentType { get; set; } = "application/pdf";

    public long FileSize { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    [StringLength(80)]
    public string? GeneratedBy { get; set; }

    public DateTime? SentAt { get; set; }

    [StringLength(150)]
    public string? SentToEmail { get; set; }

    public DateTime? DownloadedAt { get; set; }

    /// <summary>JSON of extra field values used at generation time.</summary>
    public string? ExtraFieldsJson { get; set; }
}
