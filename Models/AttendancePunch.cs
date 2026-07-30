using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class AttendancePunch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    /// <summary>In or Out</summary>
    [Required]
    [StringLength(10)]
    public string PunchType { get; set; } = "In";

    public DateTime PunchedAt { get; set; } = DateTime.Now;

    [StringLength(200)]
    public string? Notes { get; set; }

    [StringLength(80)]
    public string? PunchedBy { get; set; }
}
