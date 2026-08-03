using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    public int? ProposalId { get; set; }
    public Proposal? Proposal { get; set; }

    /// <summary>Enquiry | ClientLead | DemoRequest</summary>
    [Required]
    [StringLength(20)]
    public string LeadType { get; set; } = string.Empty;

    public int LeadId { get; set; }

    [Required]
    [StringLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Cgst { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Sgst { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Igst { get; set; }

    [StringLength(20)]
    public string? HsnSac { get; set; }

    /// <summary>Unpaid | Partial | Paid</summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Unpaid";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public DateTime? LastReminderAt { get; set; }

    public List<InvoicePayment> Payments { get; set; } = new();

    [NotMapped]
    public decimal TaxTotal => Cgst + Sgst + Igst;

    [NotMapped]
    public decimal GrandTotal => Amount + TaxTotal;

    [NotMapped]
    public decimal Balance => Math.Max(0, GrandTotal - AmountPaid);

    [NotMapped]
    public decimal PaidPercent => GrandTotal <= 0 ? 0 : Math.Min(100, Math.Round(AmountPaid / GrandTotal * 100, 1));
}
