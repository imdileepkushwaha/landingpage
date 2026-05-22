using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class EnquiryNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EnquiryId { get; set; }

    [ForeignKey("EnquiryId")]
    public Enquiry Enquiry { get; set; }

    [Required]
    public string NoteText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsPostConfirmation { get; set; } = false;
}
