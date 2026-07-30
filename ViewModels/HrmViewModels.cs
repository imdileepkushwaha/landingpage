using SoftflipSolutions.Models;

namespace SoftflipSolutions.ViewModels;

public class PunchAttendanceViewModel
{
    public int? EmployeeId { get; set; }
    public string? Notes { get; set; }
    public List<Employee> ActiveEmployees { get; set; } = new();
    public List<AttendancePunch> TodayPunches { get; set; } = new();
    public AttendancePunch? LastPunchForSelected { get; set; }
    public string SuggestedPunchType { get; set; } = "In";
}
