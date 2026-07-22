using SoftflipSolutions.Models;

namespace SoftflipSolutions.ViewModels;

public class LeadDealPanelViewModel
{
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public string Status { get; set; } = LeadPipeline.Pending;
    public string ClientName { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string? SuggestedAmount { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public Proposal? LatestProposal { get; set; }
    public Invoice? LatestInvoice { get; set; }
    public string ProposalPublicUrl { get; set; } = string.Empty;
    public List<ServiceCatalog> Services { get; set; } = new();
}
