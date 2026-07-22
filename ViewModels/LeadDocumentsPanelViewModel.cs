using SoftflipSolutions.Models;

namespace SoftflipSolutions.ViewModels;

public class LeadDocumentsPanelViewModel
{
    public string LeadType { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public bool CanEdit { get; set; } = true;
    public List<LeadDocument> Documents { get; set; } = new();
}
