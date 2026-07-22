namespace SoftflipSolutions.Models;

public static class LeadPipeline
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string ProposalSent = "ProposalSent";
    public const string Invoiced = "Invoiced";
    public const string Paid = "Paid";
    public const string Rejected = "Rejected";

    public const string LeadEnquiry = "Enquiry";
    public const string LeadClient = "ClientLead";
    public const string LeadDemo = "DemoRequest";

    public static readonly string[] ActiveDealStages =
    [
        Confirmed,
        ProposalSent,
        Invoiced,
        Paid
    ];

    public static readonly string[] PipelineSteps =
    [
        Confirmed,
        ProposalSent,
        Invoiced,
        Paid
    ];

    public static string DisplayName(string? status) => status switch
    {
        Confirmed => "Confirmed",
        ProposalSent => "Proposal Sent",
        Invoiced => "Invoiced",
        Paid => "Paid",
        Rejected => "Rejected",
        _ => "Pending"
    };

    public static string BadgeClass(string? status) => status switch
    {
        Confirmed => "success",
        ProposalSent => "info",
        Invoiced => "primary",
        Paid => "success",
        Rejected => "danger",
        _ => "warning"
    };

    public static string BadgeIcon(string? status) => status switch
    {
        Confirmed => "bi-check-circle-fill",
        ProposalSent => "bi-file-earmark-text-fill",
        Invoiced => "bi-receipt",
        Paid => "bi-cash-coin",
        Rejected => "bi-x-circle-fill",
        _ => "bi-hourglass-split"
    };

    public static bool IsActiveDeal(string? status) =>
        !string.IsNullOrEmpty(status) && ActiveDealStages.Contains(status);

    public static bool IsQualified(string? status) =>
        IsActiveDeal(status) || status == Pending || string.IsNullOrEmpty(status);

    public static int StepIndex(string? status)
    {
        if (string.IsNullOrEmpty(status) || status == Pending || status == Rejected)
            return -1;
        var idx = Array.IndexOf(PipelineSteps, status);
        return idx;
    }

    public static bool CanGenerateProposal(string? status) =>
        status == Confirmed || status == ProposalSent;

    public static bool CanConvertToInvoice(string? status) =>
        status == ProposalSent || status == Invoiced;

    public static bool CanMarkPaid(string? status) =>
        status == Invoiced;
}
