using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class MessageTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Email or WhatsApp</summary>
    [Required]
    [StringLength(20)]
    public string Channel { get; set; } = "WhatsApp";

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
