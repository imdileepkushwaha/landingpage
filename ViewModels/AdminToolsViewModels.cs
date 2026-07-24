namespace SoftflipSolutions.ViewModels;

public class DuplicateLeadMatch
{
    public string SourceType { get; set; } = string.Empty; // Enquiry | ClientLead | DemoRequest | PartnerClient
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string MatchOn { get; set; } = string.Empty; // Phone | Email
    public string Url { get; set; } = "#";
}

public class GlobalSearchResultItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public string Icon { get; set; } = "bi-search";
}

public class GlobalSearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public List<GlobalSearchResultItem> Results { get; set; } = new();
}

public class SalesSummaryReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int NewEnquiries { get; set; }
    public int NewDemos { get; set; }
    public int NewExternalLeads { get; set; }
    public int ProposalsCreated { get; set; }
    public int PartnerProposalsCreated { get; set; }
    public decimal ProposalValue { get; set; }
    public decimal PartnerProposalValue { get; set; }
    public int InvoicesCreated { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal AmountCollected { get; set; }
    public decimal Outstanding { get; set; }
    public decimal EstimatedCommission { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal CommissionPending { get; set; }
}
