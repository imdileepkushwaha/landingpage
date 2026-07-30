using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class EmployeeMenuPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required]
    [StringLength(60)]
    public string MenuKey { get; set; } = string.Empty;
}
