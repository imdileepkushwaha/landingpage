using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public static class AdminRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Sales = "Sales";
    public const string HR = "HR";
    public const string Accounts = "Accounts";
    public static readonly string[] All = [SuperAdmin, Sales, HR, Accounts];
}

public class AuditLog
{
    [Key]
    public int Id { get; set; }
    [StringLength(80)]
    public string Actor { get; set; } = "System";
    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;
    [StringLength(80)]
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    [StringLength(1000)]
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AdminNotification
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Message { get; set; }
    [StringLength(40)]
    public string Type { get; set; } = "Info"; // Info | Warning | Success | Danger
    [StringLength(260)]
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class EmailLog
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(200)]
    public string ToEmail { get; set; } = string.Empty;
    [Required, StringLength(300)]
    public string Subject { get; set; } = string.Empty;
    [StringLength(40)]
    public string Category { get; set; } = "General";
    public bool Success { get; set; }
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    [StringLength(80)]
    public string? SentBy { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
}

public class LeadTask
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(20)]
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;
    [StringLength(80)]
    public string? AssignedTo { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
}

public class LeaveRequest
{
    [Key]
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Required, StringLength(40)]
    public string LeaveType { get; set; } = "Casual"; // Casual | Sick | Earned | Unpaid
    [DataType(DataType.Date)]
    public DateTime FromDate { get; set; }
    [DataType(DataType.Date)]
    public DateTime ToDate { get; set; }
    [StringLength(500)]
    public string? Reason { get; set; }
    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    [StringLength(80)]
    public string? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReviewedAt { get; set; }

    [NotMapped]
    public int Days => Math.Max(1, (ToDate.Date - FromDate.Date).Days + 1);
}

public class CompanyHoliday
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }
    [StringLength(40)]
    public string Type { get; set; } = "Public"; // Public | Optional
    public bool IsActive { get; set; } = true;
}

public class EmployeeFile
{
    [Key]
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Required, StringLength(80)]
    public string Category { get; set; } = "Other"; // Aadhaar | PAN | Resume | Photo | Other
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(260)]
    public string FilePath { get; set; } = string.Empty;
    [StringLength(120)]
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    [StringLength(80)]
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}

public class SalaryStructure
{
    [Key]
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Basic { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Hra { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowance { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductions { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    public decimal Gross => Basic + Hra + Allowance;

    [NotMapped]
    public decimal Net => Math.Max(0, Gross - Deductions);
}

public class Payslip
{
    [Key]
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Basic { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Hra { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowance { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductions { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetPay { get; set; }
    [StringLength(260)]
    public string? FilePath { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    [StringLength(80)]
    public string? GeneratedBy { get; set; }
}

public class RecurringInvoice
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(20)]
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [StringLength(20)]
    public string Frequency { get; set; } = "Monthly"; // Monthly | Quarterly | Yearly
    public DateTime NextDueDate { get; set; } = DateTime.Today.AddMonths(1);
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastGeneratedAt { get; set; }
}
