namespace SoftflipSolutions.Models;

/// <summary>Partner client pipeline stages (Kanban).</summary>
public static class PartnerClientStages
{
    public const string New = "New";
    public const string Quotation = "Quotation";
    public const string Demo = "Demo";
    public const string Negotiation = "Negotiation";
    public const string Won = "Won";
    public const string Lost = "Lost";

    public static readonly (string Key, string Label, string Color)[] All =
    [
        (New, "New", "#0ea5e9"),
        (Quotation, "Quotation", "#8b5cf6"),
        (Demo, "Demo", "#f59e0b"),
        (Negotiation, "Negotiation", "#f97316"),
        (Won, "Won", "#22c55e"),
        (Lost, "Lost", "#94a3b8")
    ];

    public static bool IsKnown(string? stage) =>
        !string.IsNullOrWhiteSpace(stage) && All.Any(s => s.Key.Equals(stage.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? stage) =>
        IsKnown(stage) ? All.First(s => s.Key.Equals(stage!.Trim(), StringComparison.OrdinalIgnoreCase)).Key : New;
}
