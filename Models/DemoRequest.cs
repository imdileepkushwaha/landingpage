using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class DemoRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; }

    [Required]
    [StringLength(20)]
    public string Phone { get; set; }

    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; }

    [Required]
    [StringLength(100)]
    public string Requirement { get; set; }

    [Required]
    public string Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Rejected

    public List<DemoRequestNote> Notes { get; set; } = new();
}
