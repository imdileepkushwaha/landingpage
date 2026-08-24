namespace SoftflipSolutions.ViewModels;

public class AdminProposalListItem
{
    public int Id { get; set; }
    public string Source { get; set; } = "Admin"; // Admin | Partner
    public string LeadName { get; set; } = string.Empty;
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public string? PartnerName { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FilePath { get; set; }
    public bool HasInvoice { get; set; }
}

public class AdminInvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CommissionTrackerRow
{
    public int ProposalId { get; set; }
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal EstimatedCommission { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommissionTrackerViewModel
{
    public decimal TotalEstimated { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalPaid { get; set; }
    public int PendingCount { get; set; }
    public int PaidCount { get; set; }
    public List<CommissionTrackerRow> Rows { get; set; } = new();
}

public class ActivityFeedItem
{
    public DateTime At { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-circle";
    public string Accent { get; set; } = "info";
    public string Url { get; set; } = "#";
}

public class LeadFollowUpsPanelViewModel
{
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public List<FollowUpReminderItem> Items { get; set; } = new();
}

public class FollowUpReminderItem
{
    public int Id { get; set; }
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string StepType { get; set; } = SoftflipSolutions.Models.FollowUpSteps.Note;
    public DateTime DueAt { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsOverdue => !IsDone && DueAt < DateTime.Now;
}

public class LeadPartnerAssignPanelViewModel
{
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public int? PartnerClientId { get; set; }
    public int? ChannelPartnerId { get; set; }
    public string? PartnerCompanyName { get; set; }
    public string? PartnerOwnerName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? AssignNote { get; set; }
    public List<PartnerOption> Partners { get; set; } = new();
    public List<FollowUpReminderItem> PartnerFollowUps { get; set; } = new();
    public int OpenFollowUps => PartnerFollowUps.Count(x => !x.IsDone);
    public int OverdueFollowUps => PartnerFollowUps.Count(x => x.IsOverdue);
}

public class PartnerOption
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class PartnerAssignedLeadRow
{
    public int PartnerClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public int ChannelPartnerId { get; set; }
    public string PartnerCompany { get; set; } = string.Empty;
    public string SourceLeadType { get; set; } = string.Empty;
    public int SourceLeadId { get; set; }
    public string SourceLeadName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string? AssignNote { get; set; }
    public int OpenFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public DateTime? NextDueAt { get; set; }
    public string? LatestFollowUpNote { get; set; }
}

