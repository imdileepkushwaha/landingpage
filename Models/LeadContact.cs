namespace SoftflipSolutions.Models;

public class LeadContact
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public string Status { get; set; } = LeadPipeline.Pending;
}
