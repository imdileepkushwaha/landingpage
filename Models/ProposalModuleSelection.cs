using System.Text.Json.Serialization;

namespace SoftflipSolutions.Models;

/// <summary>Snapshot of modules included in a proposal (stored as JSON on Proposal).</summary>
public class ProposalModuleSelection
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subModules")]
    public List<string> SubModules { get; set; } = new();
}
