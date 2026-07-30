using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(25)]
    [Display(Name = "Mobile")]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Designation { get; set; } = string.Empty;

    [Display(Name = "Date of Joining")]
    [DataType(DataType.Date)]
    public DateTime DateOfJoining { get; set; } = DateTime.Today;

    [StringLength(400)]
    public string? Address { get; set; }

    /// <summary>Plain password for employee panel login (same pattern as partners).</summary>
    [StringLength(100)]
    public string? PasswordHash { get; set; }

    public bool CanLogin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public List<AttendancePunch> AttendancePunches { get; set; } = new();

    public List<EmployeeDocument> Documents { get; set; } = new();

    public List<EmployeeMenuPermission> MenuPermissions { get; set; } = new();

    [NotMapped]
    public string DisplayLabel => $"{EmployeeCode} — {FullName}";
}
