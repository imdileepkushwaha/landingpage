namespace SoftflipSolutions.Models;

public class ProposalTemplate
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-file-earmark-text";
    public string Accent { get; set; } = "#00aeef";
    public string TitlePattern { get; set; } = string.Empty;
    public string ScopePattern { get; set; } = string.Empty;
}

public static class ProposalTemplates
{
    public const string Classic = "classic";
    public const string Modern = "modern";
    public const string Minimal = "minimal";

    public static readonly IReadOnlyList<ProposalTemplate> All =
    [
        new ProposalTemplate
        {
            Key = Classic,
            Name = "Classic",
            Description = "Formal proposal with clear scope and investment block.",
            Icon = "bi-file-earmark-ruled",
            Accent = "#00aeef",
            TitlePattern = "Proposal for {client} — {requirement}",
            ScopePattern =
                "Dear {client},\n\n" +
                "Thank you for considering Softflip for your {requirement} requirement.\n\n" +
                "Scope of work:\n" +
                "• Requirement analysis and planning\n" +
                "• Design and development as discussed\n" +
                "• Testing, deployment support, and basic training\n\n" +
                "Timeline and milestones will be shared after kickoff. This proposal is valid for the period mentioned below."
        },
        new ProposalTemplate
        {
            Key = Modern,
            Name = "Modern",
            Description = "Outcome-focused pitch with deliverables and support.",
            Icon = "bi-lightning-charge",
            Accent = "#a6ce39",
            TitlePattern = "Solution Proposal for {client}",
            ScopePattern =
                "Hi {client},\n\n" +
                "Here’s a clear plan for your {requirement} project.\n\n" +
                "What you get:\n" +
                "• Discovery workshop and solution blueprint\n" +
                "• UI/UX + development sprints\n" +
                "• Launch checklist and 30-day hypercare\n\n" +
                "We’ll keep communication simple with weekly updates and a shared progress board."
        },
        new ProposalTemplate
        {
            Key = Minimal,
            Name = "Minimal",
            Description = "Short and clean — ideal for WhatsApp / quick email send.",
            Icon = "bi-subtract",
            Accent = "#152238",
            TitlePattern = "{requirement} — Quote",
            ScopePattern =
                "Proposal for {client}\n\n" +
                "Requirement: {requirement}\n\n" +
                "Includes: discussion-aligned deliverables, implementation, and handover.\n" +
                "Excludes: third-party licenses or unpaid add-ons unless listed separately."
        }
    ];

    public static ProposalTemplate Get(string? key) =>
        All.FirstOrDefault(t => t.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static string Fill(string pattern, string client, string requirement) =>
        pattern
            .Replace("{client}", client, StringComparison.OrdinalIgnoreCase)
            .Replace("{requirement}", requirement, StringComparison.OrdinalIgnoreCase);
}
