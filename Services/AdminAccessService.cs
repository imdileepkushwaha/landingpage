using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public interface IAdminAccessService
{
    Task<HashSet<string>> GetMenuKeysAsync(int adminUserId);
    Task<HashSet<string>> GetMenuKeysForPrincipalAsync(ClaimsPrincipal user);
    Task<bool> HasMenuAsync(ClaimsPrincipal user, string menuKey);
    Task SetMenusAsync(int adminUserId, IEnumerable<string> menuKeys);
    Task EnsureDefaultsIfEmptyAsync(int adminUserId, string? role = null);
    bool IsSuperAdmin(ClaimsPrincipal user);
}

public class AdminAccessService : IAdminAccessService
{
    private readonly ApplicationDbContext _context;

    public AdminAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public bool IsSuperAdmin(ClaimsPrincipal user) =>
        user.IsInRole(AdminRoles.SuperAdmin) ||
        string.Equals(user.FindFirstValue(ClaimTypes.Role), AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public async Task<HashSet<string>> GetMenuKeysAsync(int adminUserId)
    {
        var admin = await _context.AdminUsers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == adminUserId);
        if (admin == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.Equals(admin.Role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            return AdminMenuCatalog.AllKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = await _context.AdminMenuPermissions
            .AsNoTracking()
            .Where(p => p.AdminUserId == adminUserId)
            .Select(p => p.MenuKey)
            .ToListAsync();
        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> GetMenuKeysForPrincipalAsync(ClaimsPrincipal user)
    {
        if (IsSuperAdmin(user))
            return AdminMenuCatalog.AllKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var idClaim = user.FindFirstValue("AdminId");
        if (!int.TryParse(idClaim, out var adminId) || adminId <= 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return await GetMenuKeysAsync(adminId);
    }

    public async Task<bool> HasMenuAsync(ClaimsPrincipal user, string menuKey)
    {
        if (string.IsNullOrWhiteSpace(menuKey)) return true;
        if (IsSuperAdmin(user)) return true;
        var keys = await GetMenuKeysForPrincipalAsync(user);
        return keys.Contains(menuKey);
    }

    public async Task SetMenusAsync(int adminUserId, IEnumerable<string> menuKeys)
    {
        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null) return;

        // SuperAdmin always has everything — still store for clarity / future demotion
        var allowed = AdminMenuCatalog.AllKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = menuKeys
            .Where(k => !string.IsNullOrWhiteSpace(k) && allowed.Contains(k.Trim()))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (next.Count > 0 && !next.Contains(AdminMenuCatalog.Dashboard, StringComparer.OrdinalIgnoreCase))
            next.Insert(0, AdminMenuCatalog.Dashboard);

        var existing = await _context.AdminMenuPermissions
            .Where(p => p.AdminUserId == adminUserId)
            .ToListAsync();
        _context.AdminMenuPermissions.RemoveRange(existing);

        foreach (var key in next)
        {
            _context.AdminMenuPermissions.Add(new AdminMenuPermission
            {
                AdminUserId = adminUserId,
                MenuKey = key
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task EnsureDefaultsIfEmptyAsync(int adminUserId, string? role = null)
    {
        var any = await _context.AdminMenuPermissions.AnyAsync(p => p.AdminUserId == adminUserId);
        if (any) return;

        role ??= (await _context.AdminUsers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == adminUserId))?.Role;
        if (string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            await SetMenusAsync(adminUserId, AdminMenuCatalog.AllKeys);
        else
            await SetMenusAsync(adminUserId, AdminMenuCatalog.DefaultKeys);
    }
}
