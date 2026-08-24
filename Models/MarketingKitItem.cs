using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class MarketingKitItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Brochure | Poster | WhatsApp | Other</summary>
    [Required]
    [StringLength(40)]
    public string Category { get; set; } = "Other";

    [Required]
    [StringLength(400)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(120)]
    public string? FileName { get; set; }

    [StringLength(80)]
    public string? ContentType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int SortOrder { get; set; }
}
