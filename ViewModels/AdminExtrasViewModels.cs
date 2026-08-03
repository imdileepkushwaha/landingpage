namespace SoftflipSolutions.ViewModels;

public class PipelineCardVm
{
    public string LeadType { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    /// <summary>Short secondary line (phone, email, or requirement snippet).</summary>
    public string? Subtitle { get; set; }
}

public class MergeLeadGroupVm
{
    public string MatchKey { get; set; } = string.Empty;
    public string MatchOn { get; set; } = string.Empty;
    public List<MergeLeadItemVm> ClientLeads { get; set; } = new();
    public List<MergeLeadItemVm> RelatedEnquiries { get; set; } = new();
}

public class MergeLeadItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DetailUrl { get; set; } = string.Empty;
}

public class InvoiceReminderVm
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastReminderAt { get; set; }
}
