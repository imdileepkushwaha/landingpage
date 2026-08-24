namespace SoftflipSolutions.Models;

/// <summary>Structured follow-up step types shown on the timeline.</summary>
public static class FollowUpSteps
{
    public const string Requirement = "Requirement";
    public const string Quotation = "Quotation";
    public const string Demo = "Demo";
    public const string Call = "Call";
    public const string Meeting = "Meeting";
    public const string Negotiation = "Negotiation";
    public const string Note = "Note";
    public const string Other = "Other";

    public static readonly IReadOnlyList<FollowUpStepInfo> All =
    [
        new(Requirement, "Requirement", "Discuss / confirm client requirement", "bi-list-check", "primary"),
        new(Quotation, "Quotation", "Send quotation / proposal", "bi-file-earmark-text", "info"),
        new(Demo, "Demo", "Send or schedule product demo", "bi-display", "warning"),
        new(Call, "Call", "Phone / WhatsApp call", "bi-telephone", "success"),
        new(Meeting, "Meeting", "In-person or online meeting", "bi-people", "secondary"),
        new(Negotiation, "Negotiation", "Price / scope negotiation", "bi-cash-coin", "danger"),
        new(Note, "Note", "General note or update", "bi-chat-left-text", "dark"),
        new(Other, "Other", "Any other follow-up step", "bi-three-dots", "secondary")
    ];

    public static FollowUpStepInfo Get(string? key) =>
        All.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? All.First(s => s.Key == Other);

    public static bool IsKnown(string? key) =>
        !string.IsNullOrWhiteSpace(key) && All.Any(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}

public record FollowUpStepInfo(string Key, string Label, string Hint, string Icon, string Badge);
