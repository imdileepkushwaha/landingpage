namespace SoftflipSolutions.Services;

public record EmployeeMenuItem(
    string Key,
    string Label,
    string Icon,
    string Section,
    string Action,
    string? Description = null);

public static class EmployeeMenuCatalog
{
    public const string Dashboard = "dashboard";
    public const string Punch = "punch";
    public const string Documents = "documents";
    public const string Profile = "profile";
    public const string Team = "team";

    public static IReadOnlyList<EmployeeMenuItem> All { get; } =
    [
        new(Dashboard, "Dashboard", "bi-grid", "Main", "Index", "Home overview"),
        new(Punch, "Punch Attendance", "bi-fingerprint", "HR", "Punch", "Check in / check out"),
        new(Documents, "My Documents", "bi-folder2-open", "HR", "Documents", "Offer letters & HR PDFs"),
        new(Profile, "My Profile", "bi-person", "Account", "Profile", "View / update contact details"),
        new(Team, "Team Directory", "bi-people", "HR", "Team", "View active colleagues")
    ];

    public static IReadOnlyList<string> DefaultKeys { get; } =
        [Dashboard, Punch, Documents, Profile];

    public static EmployeeMenuItem? Find(string key) =>
        All.FirstOrDefault(m => m.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static string? KeyForAction(string? action) => action switch
    {
        "Index" => Dashboard,
        "Punch" => Punch,
        "Documents" or "DownloadDocument" => Documents,
        "Profile" => Profile,
        "Team" => Team,
        _ => null
    };
}
