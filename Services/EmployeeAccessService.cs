using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IEmployeeAccessService
{
    Task<HashSet<string>> GetMenuKeysAsync(int employeeId);
    Task<bool> HasMenuAsync(int employeeId, string menuKey);
    Task SetMenusAsync(int employeeId, IEnumerable<string> menuKeys);
    Task EnsureDefaultsIfEmptyAsync(int employeeId);
}

public class EmployeeAccessService : IEmployeeAccessService
{
    private readonly ApplicationDbContext _context;

    public EmployeeAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HashSet<string>> GetMenuKeysAsync(int employeeId)
    {
        var keys = await _context.EmployeeMenuPermissions
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => p.MenuKey)
            .ToListAsync();
        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> HasMenuAsync(int employeeId, string menuKey)
    {
        if (string.IsNullOrWhiteSpace(menuKey)) return false;
        return await _context.EmployeeMenuPermissions
            .AsNoTracking()
            .AnyAsync(p => p.EmployeeId == employeeId && p.MenuKey == menuKey);
    }

    public async Task SetMenusAsync(int employeeId, IEnumerable<string> menuKeys)
    {
        var allowed = EmployeeMenuCatalog.All.Select(m => m.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = menuKeys
            .Where(k => !string.IsNullOrWhiteSpace(k) && allowed.Contains(k.Trim()))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Dashboard is always included when any access is granted
        if (next.Count > 0 && !next.Contains(EmployeeMenuCatalog.Dashboard, StringComparer.OrdinalIgnoreCase))
            next.Insert(0, EmployeeMenuCatalog.Dashboard);

        var existing = await _context.EmployeeMenuPermissions
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync();
        _context.EmployeeMenuPermissions.RemoveRange(existing);

        foreach (var key in next)
        {
            _context.EmployeeMenuPermissions.Add(new EmployeeMenuPermission
            {
                EmployeeId = employeeId,
                MenuKey = key
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task EnsureDefaultsIfEmptyAsync(int employeeId)
    {
        var any = await _context.EmployeeMenuPermissions.AnyAsync(p => p.EmployeeId == employeeId);
        if (!any)
            await SetMenusAsync(employeeId, EmployeeMenuCatalog.DefaultKeys);
    }
}
