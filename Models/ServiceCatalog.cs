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

    /// <summary>Primary / cover image path (first uploaded).</summary>
    [StringLength(400)]
    public string? ImagePath { get; set; }

    /// <summary>JSON array of image paths under /uploads/services/…</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ImagesJson { get; set; }

    public ICollection<ServicePanel> Panels { get; set; } = new List<ServicePanel>();

    [NotMapped]
    public List<string> ImagePaths
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImagesJson))
            {
                return string.IsNullOrWhiteSpace(ImagePath)
                    ? new List<string>()
                    : new List<string> { ImagePath };
            }
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImagesJson) ?? new List<string>();
            }
            catch
            {
                return string.IsNullOrWhiteSpace(ImagePath)
                    ? new List<string>()
                    : new List<string> { ImagePath };
            }
        }
        set
        {
            var list = (value ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            ImagesJson = list.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(list);
            ImagePath = list.FirstOrDefault();
        }
    }
}
