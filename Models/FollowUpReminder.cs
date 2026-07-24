using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class FollowUpReminder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string LeadType { get; set; } = string.Empty;

    public int LeadId { get; set; }

    public DateTime DueAt { get; set; }

    [Required]
    [StringLength(500)]
    public string Note { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }
}
