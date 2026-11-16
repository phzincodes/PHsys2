using System.Collections.Immutable;
using PHsystem.Core;
using PHsystem.Desktop;
using Xunit;

namespace PHsystem.UiTests;

public sealed class ResultsTests
{
    [Fact]
    public void ComparisonDistinguishesMissingEvidenceFromAChangedValue()
    {
        CategoryResult Category(Dictionary<string, string> facts) => new("CPU", CollectionState.Partial, DateTimeOffset.UtcNow, "fixture", 1, [], facts.ToImmutableDictionary());
        var before = new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", [Category(new() { ["Model"] = "Same CPU", ["Load"] = "50" })]);
        var after = before with { ScanId = Guid.NewGuid(), Categories = [Category(new() { ["Model"] = "Same CPU", ["Threads"] = "8" })] };
        var changes = SnapshotComparison.Compare(before, after);
        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, c => c.Field == "Load" && c.After == "Sem dado");
        Assert.Contains(changes, c => c.Field == "Threads" && c.Before == "Sem dado");
        Assert.Empty(SnapshotComparison.Compare(before, before));
        Assert.Equal("50", before.Categories[0].Facts!["Load"]);
    }

    [Fact]
    public void NavigationValidatesDestinationsAndDoesNotRepeatChanges()
    {
        var navigation = new NavigationService(); var calls = 0;
        navigation.Changed += (_, _) => calls++;
        navigation.Navigate(AppPage.Hardware); navigation.Navigate(AppPage.Hardware);
        Assert.Equal(1, calls);
        Assert.Equal(AppPage.Hardware, navigation.Current);
        Assert.Throws<ArgumentOutOfRangeException>(() => navigation.Navigate((AppPage)100));
    }
}
