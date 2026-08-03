using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "EmployeeCookie")]
public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeAccessService _access;
    private readonly IWebHostEnvironment _env;

    public EmployeeController(
        ApplicationDbContext context,
        IEmployeeAccessService access,
        IWebHostEnvironment env)
    {
        _context = context;
        _access = access;
        _env = env;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == "EmployeeId"))
            return RedirectToAction(nameof(Index));
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var employee = await _context.Employees.FirstOrDefaultAsync(e =>
            e.Email == email &&
            e.IsActive &&
            e.CanLogin);

        if (employee == null)
        {
            ViewBag.Error = "Invalid email or password, or login is disabled for this account.";
            return View();
        }

        var hash = employee.PasswordHash;
        if (!PasswordHelper.VerifyAndUpgrade(password, ref hash, out var upgraded))
        {
            ViewBag.Error = "Invalid email or password, or login is disabled for this account.";
            return View();
        }

        if (upgraded)
        {
            employee.PasswordHash = hash!;
            await _context.SaveChangesAsync();
        }

        await _access.EnsureDefaultsIfEmptyAsync(employee.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, employee.FullName),
            new(ClaimTypes.Email, employee.Email),
            new("EmployeeId", employee.Id.ToString()),
            new("EmployeeCode", employee.EmployeeCode)
        };
        var identity = new ClaimsIdentity(claims, "EmployeeCookie");
        await HttpContext.SignInAsync("EmployeeCookie", new ClaimsPrincipal(identity));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("EmployeeCookie");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Index()
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Dashboard)) return ForbidOrRedirect();

        await PrepareLayoutAsync(employee);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        ViewBag.TodayPunches = await _context.AttendancePunches
            .Where(p => p.EmployeeId == employee.Id && p.PunchedAt >= today && p.PunchedAt < tomorrow)
            .OrderByDescending(p => p.PunchedAt)
            .ToListAsync();
        ViewBag.DocCount = await _context.EmployeeDocuments.CountAsync(d => d.EmployeeId == employee.Id);
        ViewBag.LastPunch = await _context.AttendancePunches
            .Where(p => p.EmployeeId == employee.Id)
            .OrderByDescending(p => p.PunchedAt)
            .FirstOrDefaultAsync();

        return View(employee);
    }

    public async Task<IActionResult> Punch()
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Punch)) return ForbidOrRedirect();
        await PrepareLayoutAsync(employee);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var lastToday = await _context.AttendancePunches
            .Where(p => p.EmployeeId == employee.Id && p.PunchedAt >= today && p.PunchedAt < tomorrow)
            .OrderByDescending(p => p.PunchedAt)
            .FirstOrDefaultAsync();

        ViewBag.LastPunch = lastToday;
        ViewBag.SuggestedPunchType = lastToday?.PunchType == "In" ? "Out" : "In";
        ViewBag.TodayPunches = await _context.AttendancePunches
            .Where(p => p.EmployeeId == employee.Id && p.PunchedAt >= today && p.PunchedAt < tomorrow)
            .OrderByDescending(p => p.PunchedAt)
            .ToListAsync();

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Punch(string punchType, string? notes)
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Punch)) return ForbidOrRedirect();

        var type = string.Equals(punchType, "Out", StringComparison.OrdinalIgnoreCase) ? "Out" : "In";
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var lastToday = await _context.AttendancePunches
            .Where(p => p.EmployeeId == employee.Id && p.PunchedAt >= today && p.PunchedAt < tomorrow)
            .OrderByDescending(p => p.PunchedAt)
            .FirstOrDefaultAsync();

        if (lastToday != null && lastToday.PunchType == type)
        {
            TempData["ErrorMessage"] = $"You already punched {type} today at {lastToday.PunchedAt:hh:mm tt}.";
            return RedirectToAction(nameof(Punch));
        }

        if (type == "Out" && (lastToday == null || lastToday.PunchType != "In"))
        {
            TempData["ErrorMessage"] = "Punch In first before Punch Out.";
            return RedirectToAction(nameof(Punch));
        }

        _context.AttendancePunches.Add(new AttendancePunch
        {
            EmployeeId = employee.Id,
            PunchType = type,
            PunchedAt = DateTime.Now,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PunchedBy = employee.FullName
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Punch {type} recorded at {DateTime.Now:hh:mm tt}.";
        return RedirectToAction(nameof(Punch));
    }

    public async Task<IActionResult> Documents()
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Documents)) return ForbidOrRedirect();
        await PrepareLayoutAsync(employee);

        var docs = await _context.EmployeeDocuments
            .Where(d => d.EmployeeId == employee.Id)
            .OrderByDescending(d => d.GeneratedAt)
            .ToListAsync();
        return View(docs);
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Documents)) return ForbidOrRedirect();

        var doc = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.EmployeeId == employee.Id);
        if (doc == null) return NotFound();

        var relative = doc.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (!relative.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        if (!System.IO.File.Exists(full)) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(full);
        return File(bytes, doc.ContentType ?? "application/pdf", doc.Title + ".pdf");
    }

    public async Task<IActionResult> Profile()
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Profile)) return ForbidOrRedirect();
        await PrepareLayoutAsync(employee);
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string mobile, string? address, string? newPassword, string? confirmPassword)
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Profile)) return ForbidOrRedirect();

        if (string.IsNullOrWhiteSpace(mobile))
        {
            TempData["ErrorMessage"] = "Mobile is required.";
            return RedirectToAction(nameof(Profile));
        }

        employee.Mobile = mobile.Trim();
        employee.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Trim().Length < 4)
            {
                TempData["ErrorMessage"] = "Password must be at least 4 characters.";
                return RedirectToAction(nameof(Profile));
            }
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction(nameof(Profile));
            }
            employee.PasswordHash = newPassword.Trim();
        }

        employee.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Team()
    {
        var employee = await CurrentEmployeeAsync();
        if (employee == null) return RedirectToAction(nameof(Login));
        if (!await EnsureMenuAsync(EmployeeMenuCatalog.Team)) return ForbidOrRedirect();
        await PrepareLayoutAsync(employee);

        var team = await _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Department)
            .ThenBy(e => e.FullName)
            .ToListAsync();

        return View(team);
    }

    private async Task<Employee?> CurrentEmployeeAsync()
    {
        var idClaim = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(idClaim, out var id)) return null;
        return await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.CanLogin);
    }

    private async Task PrepareLayoutAsync(Employee employee)
    {
        ViewBag.Employee = employee;
        ViewBag.AllowedMenus = await _access.GetMenuKeysAsync(employee.Id);
    }

    private async Task<bool> EnsureMenuAsync(string menuKey)
    {
        var idClaim = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(idClaim, out var id)) return false;
        return await _access.HasMenuAsync(id, menuKey);
    }

    private IActionResult ForbidOrRedirect()
    {
        TempData["ErrorMessage"] = "You do not have access to this menu. Contact admin.";
        return RedirectToAction(nameof(Index));
    }
}
