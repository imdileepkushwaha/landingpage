using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class ServiceCatalog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Service name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(2000)]
    [Display(Name = "Demo link")]
    public string? DemoLink { get; set; }

    [Required]
    [Range(0.01, 999999999)]
    [Display(Name = "Budget (₹)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Budget { get; set; }

    [Required]
    [Range(0, 100)]
    [Display(Name = "Commission (%)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Commission { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<ServicePanel> Panels { get; set; } = new List<ServicePanel>();
}
