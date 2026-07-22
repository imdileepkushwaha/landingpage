namespace SoftflipSolutions.ViewModels;

public class PartnerPerformanceViewModel
{
    public int TotalPartners { get; set; }
    public int ActivePartners { get; set; }
    public int TotalClients { get; set; }
    public int TotalProposals { get; set; }
    public decimal TotalProposalAmount { get; set; }
    public decimal TotalEstimatedCommission { get; set; }
    public List<PartnerPerformanceRow> Rows { get; set; } = new();
}

public class PartnerPerformanceRow
{
    public int PartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ClientCount { get; set; }
    public int ProposalCount { get; set; }
    public decimal ProposalAmount { get; set; }
    public decimal EstimatedCommission { get; set; }
    public DateTime? LastProposalAt { get; set; }
}
