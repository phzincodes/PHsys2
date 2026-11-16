using PHsystem.Core;

namespace PHsystem.Drivers;

public sealed record DriverInfo(string Name, string VersionAndProvider);

public sealed class DriverInventory
{
    public IReadOnlyList<DriverInfo> Read(DiagnosticSnapshot? snapshot)
    {
        var category = snapshot?.Categories.FirstOrDefault(c => c.Category == "Drivers");
        if (category?.Facts is null || category.State is CollectionState.Error or CollectionState.Unavailable) return [];
        return category.Facts.Where(f => f.Key != "Escopo").OrderBy(f => f.Key, StringComparer.Ordinal)
            .Select(f => new DriverInfo(f.Key, f.Value)).ToArray();
    }
}
