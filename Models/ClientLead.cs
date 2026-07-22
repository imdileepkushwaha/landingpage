using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class ClientLead
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mobile number — also used as WhatsApp.</summary>
    [Required]
    [StringLength(25)]
    [Display(Name = "Mobile / WhatsApp")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Requirement { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Budget { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Rejected

    public List<ClientLeadNote> Notes { get; set; } = new();
}
