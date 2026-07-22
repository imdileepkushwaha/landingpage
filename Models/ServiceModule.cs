using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class ServiceModule
{
    [Key]
    public int Id { get; set; }

    public int ServicePanelId { get; set; }
    public ServicePanel Panel { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ServiceSubModule> SubModules { get; set; } = new List<ServiceSubModule>();
}
