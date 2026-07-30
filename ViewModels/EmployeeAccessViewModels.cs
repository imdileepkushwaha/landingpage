using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.ViewModels;

public class EmployeeAccessSettingsViewModel
{
    public List<Employee> Employees { get; set; } = new();
    public int? SelectedEmployeeId { get; set; }
    public Employee? SelectedEmployee { get; set; }
    public bool CanLogin { get; set; }
    public HashSet<string> SelectedMenus { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<EmployeeMenuItem> MenuCatalog { get; set; } = EmployeeMenuCatalog.All;
    public string EmployeeLoginUrl { get; set; } = "/Employee/Login";
}
