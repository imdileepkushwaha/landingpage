using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public static class ProposalModuleSelectionHelper
{
    public static readonly string[] DefaultPanelNames =
    [
        "Admin Panel",
        "User Panel",
        "Franchise Panel"
    ];

    public static void EnsureDefaultPanels(ServiceCatalog service)
    {
        if (service.Panels.Count > 0) return;
        for (var i = 0; i < DefaultPanelNames.Length; i++)
        {
            service.Panels.Add(new ServicePanel
            {
                Name = DefaultPanelNames[i],
                SortOrder = i
            });
        }
    }

    public static async Task<List<ServiceCatalog>> GetActiveServicesAsync(ApplicationDbContext context) =>
        await context.ServiceCatalogs
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Panels.OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Modules.OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.SubModules.OrderBy(sm => sm.SortOrder))
            .OrderBy(s => s.Name)
            .ToListAsync();

    public static async Task<(int? ServiceId, string? ModulesJson)> BuildSelectionAsync(
        ApplicationDbContext context,
        int? serviceId,
        int[]? moduleIds,
        int[]? subModuleIds)
    {
        if (serviceId == null || serviceId <= 0)
            return (null, null);

        var service = await context.ServiceCatalogs
            .AsNoTracking()
            .Include(s => s.Panels)
                .ThenInclude(p => p.Modules)
                    .ThenInclude(m => m.SubModules)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);
        if (service == null)
            return (null, null);

        var modSet = new HashSet<int>(moduleIds ?? Array.Empty<int>());
        var subSet = new HashSet<int>(subModuleIds ?? Array.Empty<int>());

        var selected = new List<ProposalModuleSelection>();
        foreach (var panel in service.Panels.OrderBy(p => p.SortOrder))
        {
            var features = new List<string>();
            foreach (var module in panel.Modules.OrderBy(m => m.SortOrder))
            {
                var pickedSubs = module.SubModules
                    .Where(sm => subSet.Contains(sm.Id))
                    .OrderBy(sm => sm.SortOrder)
                    .Select(sm => sm.Name)
                    .ToList();

                var includeModule = modSet.Contains(module.Id) || pickedSubs.Count > 0;
                if (!includeModule) continue;

                if (pickedSubs.Count == 0)
                    features.Add(module.Name);
                else
                    features.AddRange(pickedSubs.Select(s => $"{module.Name} — {s}"));
            }

            if (features.Count == 0) continue;

            selected.Add(new ProposalModuleSelection
            {
                Name = panel.Name,
                SubModules = features
            });
        }

        if (selected.Count == 0)
            return (service.Id, null);

        return (service.Id, System.Text.Json.JsonSerializer.Serialize(selected));
    }

    /// <summary>
    /// Includes every panel with all features (partner proposals — view-only catalog).
    /// </summary>
    public static async Task<(int? ServiceId, string? ModulesJson)> BuildFullServiceSelectionAsync(
        ApplicationDbContext context,
        int? serviceId)
    {
        if (serviceId == null || serviceId <= 0)
            return (null, null);

        var service = await context.ServiceCatalogs
            .AsNoTracking()
            .Include(s => s.Panels)
                .ThenInclude(p => p.Modules)
                    .ThenInclude(m => m.SubModules)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);
        if (service == null)
            return (null, null);

        var selected = new List<ProposalModuleSelection>();
        foreach (var panel in service.Panels.OrderBy(p => p.SortOrder))
        {
            var features = new List<string>();
            foreach (var module in panel.Modules.OrderBy(m => m.SortOrder))
            {
                if (module.SubModules == null || module.SubModules.Count == 0)
                {
                    features.Add(module.Name);
                    continue;
                }

                features.AddRange(module.SubModules
                    .OrderBy(sm => sm.SortOrder)
                    .Select(sm => $"{module.Name} — {sm.Name}"));
            }

            if (features.Count == 0) continue;

            selected.Add(new ProposalModuleSelection
            {
                Name = panel.Name,
                SubModules = features
            });
        }

        if (selected.Count == 0)
            return (service.Id, null);

        return (service.Id, System.Text.Json.JsonSerializer.Serialize(selected));
    }
}
