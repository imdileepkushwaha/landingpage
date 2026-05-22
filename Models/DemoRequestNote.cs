using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class DemoRequestNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DemoRequestId { get; set; }

    [ForeignKey("DemoRequestId")]
    public DemoRequest DemoRequest { get; set; }

    [Required]
    public string NoteText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsPostConfirmation { get; set; } = false;
}
