using System.Collections.Immutable;
using PHsystem.Cleanup;
using PHsystem.Community;
using PHsystem.Core;
using Xunit;

namespace PHsystem.SecurityTests;

[Trait("Platform", "Windows")]
public sealed class LocalSafetyTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "PHsystem-safety-tests", Guid.NewGuid().ToString("N"));
    public LocalSafetyTests() => Directory.CreateDirectory(directory);

    [Fact]
    public async Task CleanupRequiresPreviewAndOnlyDeletesUnchangedOldTopLevelFiles()
    {
        var old = Write("old.tmp", old: true);
        var recent = Write("recent.tmp", old: false);
        var changed = Write("changed.tmp", old: true);
        var nestedDir = Directory.CreateDirectory(Path.Combine(directory, "nested"));
        var nested = Path.Combine(nestedDir.FullName, "keep.tmp");
        File.WriteAllText(nested, "keep");
        File.SetLastWriteTimeUtc(nested, DateTime.UtcNow.AddDays(-10));
        var service = new TempCleanupService(directory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(Guid.NewGuid()));
        var plan = await service.PreviewAsync();
        Assert.Equal(2, plan.Files.Length);
        Assert.True(File.Exists(old));
        File.AppendAllText(changed, "changed");
        var outcome = await service.ExecuteAsync(plan.Id);
        Assert.Equal(1, outcome.Deleted);
        Assert.Equal(1, outcome.Skipped);
        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
        Assert.True(File.Exists(changed));
        Assert.True(File.Exists(nested));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(plan.Id));
    }

    [Fact]
    public async Task LockedFilesArePreserved()
    {
        var file = Write("locked.tmp", true);
        var service = new TempCleanupService(directory);
        var plan = await service.PreviewAsync();
        using var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var outcome = await service.ExecuteAsync(plan.Id);
        Assert.Equal(0, outcome.Deleted);
        Assert.Equal(1, outcome.Skipped);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void CommunityDraftExcludesPrivateFactsAndRawErrors()
    {
        var snapshot = new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", [new("Memória", CollectionState.Error,
            DateTimeOffset.UtcNow, "secret-source", 0, ["secret-path"], new Dictionary<string, string> { ["serial"] = "secret-serial" }.ToImmutableDictionary())]);
        var exported = CommunityDraftBuilder.Export(CommunityDraftBuilder.Create(snapshot));
        Assert.DoesNotContain("secret", exported);
        Assert.DoesNotContain(snapshot.ScanId.ToString(), exported);
        Assert.Contains("Error", exported);
    }

    private string Write(string name, bool old)
    {
        var file = Path.Combine(directory, name);
        File.WriteAllText(file, "temporary test data");
        if (old) File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(-10));
        return file;
    }
    public void Dispose() => Directory.Delete(directory, true);

    [Fact]
    public void VolumeRootIsNeverAnEligibleCleanupDirectory() =>
        Assert.Throws<ArgumentException>(() => new TempCleanupService(Path.GetPathRoot(directory)!));

    [Fact]
    public async Task StorageAnalysisIsReadOnlyAndHashingIsOptIn()
    {
        var first = Write("copy1.bin", false);
        var second = Write("copy2.bin", false);
        var analyzer = new StorageAnalyzer();
        var metadata = await analyzer.AnalyzeAsync(directory, compareHashes: false);
        Assert.Equal(2, metadata.FilesObserved);
        Assert.Equal(0, metadata.FilesHashed);
        Assert.Empty(metadata.Duplicates);
        var hashed = await analyzer.AnalyzeAsync(directory, compareHashes: true);
        Assert.Equal(2, hashed.FilesHashed);
        Assert.Equal(2, Assert.Single(hashed.Duplicates).Paths.Count);
        Assert.True(File.Exists(first)); Assert.True(File.Exists(second));
    }

    [Fact]
    public async Task JunctionRootIsRejectedAndOutsideFileIsPreserved()
    {
        var outside = Directory.CreateDirectory(Path.Combine(directory, "outside"));
        var targetFile = Path.Combine(outside.FullName, "personal.txt");
        File.WriteAllText(targetFile, "keep private test data");
        File.SetLastWriteTimeUtc(targetFile, DateTime.UtcNow.AddDays(-20));
        var junction = Path.Combine(directory, "junction");
        var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/d /c mklink /J \"{junction}\" \"{outside.FullName}\"",
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        using var process = System.Diagnostics.Process.Start(start)!;
        await process.WaitForExitAsync();
        Assert.Equal(0, process.ExitCode);
        try
        {
            var cleanup = new TempCleanupService(junction);
            await Assert.ThrowsAsync<IOException>(() => cleanup.PreviewAsync());
            Assert.True(File.Exists(targetFile));
            var outerPlan = await new TempCleanupService(directory).PreviewAsync();
            Assert.DoesNotContain(outerPlan.Files, f => f.Name == "personal.txt");
        }
        finally { Directory.Delete(junction); }
    }
}
