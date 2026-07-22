using ClosedXML.Excel;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Services;

public static class ServiceModuleExcelParser
{
    /// <summary>
    /// Expects columns: Feature | Sub Feature (header optional).
    /// Within a panel, Feature becomes a module; Sub Feature attaches under it.
    /// </summary>
    public static List<(string Feature, string? SubFeature)> Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RangeUsed()?.RowsUsed()?.ToList() ?? new List<IXLRangeRow>();
        if (rows.Count == 0)
            return new List<(string, string?)>();

        var startIndex = 0;
        var firstA = CellText(rows[0].Cell(1));
        var firstB = CellText(rows[0].Cell(2));
        if (LooksLikeHeader(firstA, firstB))
            startIndex = 1;

        var result = new List<(string Feature, string? SubFeature)>();
        string? lastFeature = null;

        for (var i = startIndex; i < rows.Count; i++)
        {
            var feature = CellText(rows[i].Cell(1));
            var sub = CellText(rows[i].Cell(2));

            if (string.IsNullOrWhiteSpace(feature) && string.IsNullOrWhiteSpace(sub))
                continue;

            if (string.IsNullOrWhiteSpace(feature))
                feature = lastFeature ?? "";

            if (string.IsNullOrWhiteSpace(feature))
                continue;

            lastFeature = feature;
            result.Add((feature.Trim(), string.IsNullOrWhiteSpace(sub) ? null : sub.Trim()));
        }

        return result;
    }

    public static void ApplyToPanel(ServicePanel panel, IEnumerable<(string Feature, string? SubFeature)> rows)
    {
        panel.Modules.Clear();
        var moduleMap = new Dictionary<string, ServiceModule>(StringComparer.OrdinalIgnoreCase);
        var order = 0;

        foreach (var (featureName, subName) in rows)
        {
            if (!moduleMap.TryGetValue(featureName, out var module))
            {
                module = new ServiceModule
                {
                    Name = featureName,
                    SortOrder = order++,
                    SubModules = new List<ServiceSubModule>()
                };
                moduleMap[featureName] = module;
                panel.Modules.Add(module);
            }

            if (string.IsNullOrWhiteSpace(subName))
                continue;

            if (module.SubModules.Any(s => s.Name.Equals(subName, StringComparison.OrdinalIgnoreCase)))
                continue;

            module.SubModules.Add(new ServiceSubModule
            {
                Name = subName,
                SortOrder = module.SubModules.Count
            });
        }
    }

    public static byte[] CreateSampleTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Features");
        sheet.Cell(1, 1).Value = "Feature";
        sheet.Cell(1, 2).Value = "Sub Feature";
        sheet.Cell(2, 1).Value = "Dashboard";
        sheet.Cell(2, 2).Value = "";
        sheet.Cell(3, 1).Value = "Genealogy";
        sheet.Cell(3, 2).Value = "Binary tree";
        sheet.Cell(4, 1).Value = "Genealogy";
        sheet.Cell(4, 2).Value = "Sponsor view";
        sheet.Cell(5, 1).Value = "Payouts";
        sheet.Cell(5, 2).Value = "Weekly payout";
        sheet.Cell(6, 1).Value = "E-wallet";
        sheet.Cell(6, 2).Value = "";
        sheet.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string CellText(IXLCell cell) =>
        cell.GetString()?.Trim() ?? string.Empty;

    private static bool LooksLikeHeader(string a, string b)
    {
        var combined = $"{a} {b}".ToLowerInvariant();
        return combined.Contains("feature") || combined.Contains("module") || combined.Contains("sub");
    }
}
