using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class ServiceSubModule
{
    [Key]
    public int Id { get; set; }

    public int ServiceModuleId { get; set; }
    public ServiceModule Module { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
