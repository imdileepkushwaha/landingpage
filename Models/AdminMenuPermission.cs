using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class AdminMenuPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AdminUserId { get; set; }

    [ForeignKey(nameof(AdminUserId))]
    public AdminUser? AdminUser { get; set; }

    [Required]
    [StringLength(60)]
    public string MenuKey { get; set; } = string.Empty;
}
