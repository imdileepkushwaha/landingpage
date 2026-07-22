using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class ServicePanel
{
    [Key]
    public int Id { get; set; }

    public int ServiceCatalogId { get; set; }
    public ServiceCatalog Service { get; set; } = null!;

    [Required]
    [StringLength(120)]
    [Display(Name = "Panel name")]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ServiceModule> Modules { get; set; } = new List<ServiceModule>();
}
