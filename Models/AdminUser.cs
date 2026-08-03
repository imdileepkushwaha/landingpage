using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class AdminUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>SuperAdmin | Sales | HR | Accounts</summary>
    [Required]
    [StringLength(30)]
    public string Role { get; set; } = AdminRoles.SuperAdmin;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<AdminMenuPermission> MenuPermissions { get; set; } = new();
}
