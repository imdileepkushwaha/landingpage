using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class AdminSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; }

    [Required]
    public string Value { get; set; }
}
