using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class EmployeeDocumentTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>OfferLetter | Appointment | Experience | Relieving | Warning | Custom</summary>
    [Required]
    [StringLength(40)]
    [Display(Name = "Document type")]
    public string DocumentType { get; set; } = "Custom";

    [StringLength(200)]
    public string? Subject { get; set; }

    /// <summary>Letter body with placeholders like {{EmployeeName}}, {{Designation}}.</summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public List<EmployeeDocument> Documents { get; set; } = new();
}
