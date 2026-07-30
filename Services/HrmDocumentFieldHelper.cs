namespace SoftflipSolutions.Services;

public static class HrmDocumentFieldHelper
{
    public static readonly string[] AllPlaceholders =
    [
        "EmployeeName", "EmployeeCode", "Designation", "Department", "Mobile", "Email",
        "Address", "JoiningDate", "Date", "CompanyName", "CompanyAddress", "CompanyPhone",
        "CompanyEmail", "CompanyWebsite", "SignatoryName", "SignatoryTitle",
        "Amount", "ReportingTime", "ProbationMonths", "WorkingHours", "WorkingDays",
        "NoticeDays", "Reason", "LastWorkingDate", "FromDate", "ToDate"
    ];

    public static bool ShowOfferFields(string? documentType) =>
        documentType is "OfferLetter" or "Appointment";

    public static bool ShowRelievingFields(string? documentType) =>
        documentType is "Relieving" or "Experience";

    public static bool ShowWarningFields(string? documentType) =>
        documentType == "Warning";

    public static string IconForType(string? documentType) => documentType switch
    {
        "OfferLetter" => "bi-file-earmark-check",
        "Appointment" => "bi-person-check",
        "Experience" => "bi-award",
        "Relieving" => "bi-box-arrow-right",
        "Warning" => "bi-exclamation-triangle",
        _ => "bi-file-earmark-text"
    };

    public static string ColorClassForType(string? documentType) => documentType switch
    {
        "OfferLetter" => "text-success",
        "Appointment" => "text-info",
        "Experience" => "text-primary",
        "Relieving" => "text-warning",
        "Warning" => "text-danger",
        _ => "text-muted"
    };

    public static string AccentKeyForType(string? documentType) => documentType switch
    {
        "OfferLetter" => "offer",
        "Appointment" => "appointment",
        "Experience" => "experience",
        "Relieving" => "relieving",
        "Warning" => "warning",
        _ => "custom"
    };

    public static string LabelForType(string? documentType) => documentType switch
    {
        "OfferLetter" => "Offer letter",
        "Appointment" => "Appointment",
        "Experience" => "Experience",
        "Relieving" => "Relieving",
        "Warning" => "Warning",
        _ => string.IsNullOrWhiteSpace(documentType) ? "Document" : documentType
    };

    public static string StatusLabel(DateTime? sentAt, DateTime? downloadedAt) =>
        sentAt.HasValue ? "Sent" :
        downloadedAt.HasValue ? "Downloaded" :
        "Draft";

    public static string StatusBadgeClass(DateTime? sentAt, DateTime? downloadedAt) =>
        sentAt.HasValue ? "bg-success bg-opacity-25 text-success" :
        downloadedAt.HasValue ? "bg-info bg-opacity-25 text-info" :
        "bg-secondary bg-opacity-25 text-secondary";
}
