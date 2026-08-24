using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftflipSolutions.Models;

public class PartnerInvoice
{
    [Key]
    public int Id { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }

    public int PartnerClientId { get; set; }
    public PartnerClient? PartnerClient { get; set; }

    public int? PartnerProposalId { get; set; }
    public PartnerProposal? PartnerProposal { get; set; }

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

    [NotMapped]
    public decimal TaxTotal => Cgst + Sgst + Igst;

    [NotMapped]
    public decimal GrandTotal => Amount + TaxTotal;

    [NotMapped]
    public decimal Balance => Math.Max(0, GrandTotal - AmountPaid);

    public Invoice ToPdfInvoice() => new()
    {
        InvoiceNumber = InvoiceNumber,
        Title = Title,
        Description = Description,
        Amount = Amount,
        AmountPaid = AmountPaid,
        Cgst = Cgst,
        Sgst = Sgst,
        Igst = Igst,
        HsnSac = HsnSac,
        Status = Status,
        CreatedAt = CreatedAt,
        PaidAt = PaidAt
    };
}
