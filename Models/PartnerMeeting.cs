using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

/// <summary>
/// Softflip meeting shared with selected partners (or all). Visible only while MeetingAt is in the future.
/// </summary>
public class PartnerMeeting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Meeting link")]
    public string MeetingLink { get; set; } = string.Empty;

    /// <summary>Meeting date/time — after this moment the link is hidden from partners.</summary>
    [Required]
    [Display(Name = "Meeting date & time")]
    public DateTime MeetingAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>When true, every active partner sees the meeting.</summary>
    public bool AssignToAllPartners { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public List<PartnerMeetingAssignment> Assignments { get; set; } = new();

    public bool IsVisibleToPartners => IsActive && MeetingAt >= DateTime.Now;
}
