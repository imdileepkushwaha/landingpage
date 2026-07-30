using SoftflipSolutions.Models;

namespace SoftflipSolutions.ViewModels;

public class GenerateEmployeeDocumentViewModel
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int TemplateId { get; set; }
    public List<EmployeeDocumentTemplate> Templates { get; set; } = new();

    public string? Amount { get; set; }
    public string? ReportingTime { get; set; } = "10:00 AM";
    public string? ProbationMonths { get; set; } = "3";
    public string? WorkingHours { get; set; } = "10:00 AM to 7:00 PM";
    public string? WorkingDays { get; set; } = "Monday to Saturday";
    public string? NoticeDays { get; set; } = "15";
    public string? Reason { get; set; }
    public string? LastWorkingDate { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public string? CustomTitle { get; set; }

    /// <summary>save | email | preview (preview is GET/POST separate)</summary>
    public string SubmitAction { get; set; } = "save";
}

public class BulkGenerateDocumentsViewModel
{
    public int TemplateId { get; set; }
    public List<EmployeeDocumentTemplate> Templates { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public int[] SelectedEmployeeIds { get; set; } = Array.Empty<int>();

    public string? Amount { get; set; }
    public string? ReportingTime { get; set; } = "10:00 AM";
    public string? ProbationMonths { get; set; } = "3";
    public string? WorkingHours { get; set; } = "10:00 AM to 7:00 PM";
    public string? WorkingDays { get; set; } = "Monday to Saturday";
    public string? NoticeDays { get; set; } = "15";
    public string? Reason { get; set; }
    public string? LastWorkingDate { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public bool AlsoEmail { get; set; }
}

public class EmployeeDocumentsPanelViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public List<EmployeeDocument> Documents { get; set; } = new();
    public bool HasActiveTemplates { get; set; }
}
