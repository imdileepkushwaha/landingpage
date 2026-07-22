using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class ClientLeadNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClientLeadId { get; set; }

    [ForeignKey(nameof(ClientLeadId))]
    public ClientLead ClientLead { get; set; } = null!;

    [Required]
    public string NoteText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsPostConfirmation { get; set; } = false;
}
