using System.Runtime.InteropServices;
using PHsystem.Cleanup;
using Xunit;

namespace PHsystem.SecurityTests;

[Trait("Platform", "Windows")]
public sealed class CleanupAnalysisTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PHsystem-cleanup-v2", Guid.NewGuid().ToString("N"));
    private readonly Clock clock = new();
    public CleanupAnalysisTests() => Directory.CreateDirectory(root);
    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(root, true);
    }
    private string FileAt(string name, DateTime time)
    { var path = Path.Combine(root, name); File.WriteAllText(path, "fixture"); File.SetLastWriteTimeUtc(path, time); return path; }
    [Fact]
    public async Task ZeroIsExplainedAndSevenDayBoundaryIsExclusive()
    {
        FileAt("boundary.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-7));
        FileAt("recent.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-1));
        var protectedFile = FileAt("protected.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-10)); File.SetAttributes(protectedFile, FileAttributes.ReadOnly);
        Directory.CreateDirectory(Path.Combine(root, "subdirectory"));
        var service = new TempCleanupService(root, clock); var plan = await service.PreviewAsync();
        Assert.Empty(plan.Files); Assert.Equal(3, plan.Skipped);
        var counts = Assert.IsType<CleanupAnalysis>(plan.Analysis);
        Assert.Equal(2, counts.Count(CleanupSkipReason.Recent)); Assert.Equal(1, counts.Count(CleanupSkipReason.Protected));
        Assert.Equal(1, counts.DirectoriesExamined); Assert.Equal(1, counts.DirectoriesExcluded);
        Assert.Equal(counts.FilesConsidered, counts.Eligible + counts.Ignored);
        Assert.Equal(counts.EntriesExamined, counts.FilesConsidered + counts.DirectoriesExcluded + counts.UnidentifiedEntries);
        Assert.False(counts.IsPartial);
    }
    [Fact]
    public async Task LockedFilesAndHardLinksAreNotPresentedAsEligible()
    {
        var locked = FileAt("locked.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-9));
        var original = FileAt("original.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-9));
        Assert.True(CreateHardLink(Path.Combine(root, "linked.tmp"), original, IntPtr.Zero));
        using var handle = File.Open(locked, FileMode.Open, FileAccess.Read, FileShare.None);
        var plan = await new TempCleanupService(root, clock).PreviewAsync();
        Assert.Empty(plan.Files); Assert.Equal(1, plan.Analysis!.Count(CleanupSkipReason.InUse));
        Assert.Equal(2, plan.Analysis.Count(CleanupSkipReason.HardLink));
        Assert.Equal(3, plan.Analysis.Ignored);
    }
    [Fact]
    public async Task ReplacedFileWithSameSizeAndTimestampIsPreservedByIdentity()
    {
        var path = FileAt("replace.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-10));
        var service = new TempCleanupService(root, clock); var plan = await service.PreviewAsync();
        Directory.CreateDirectory(Path.Combine(root, "preserved")); File.Move(path, Path.Combine(root, "preserved", "original.tmp"));
        FileAt("replace.tmp", plan.Files.Single().LastWriteUtc);
        var result = await service.ExecuteAsync(plan.Id);
        Assert.Equal(0, result.Deleted); Assert.Equal(1, result.Skipped); Assert.True(File.Exists(path));
        Assert.False(Assert.Single(result.Items).Removed);
    }
    [Fact]
    public async Task SelectionMustMatchPlanAndResultsCountOnlyConfirmedDeletions()
    {
        var first = FileAt("first.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-10));
        var second = FileAt("second.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-10));
        var service = new TempCleanupService(root, clock); var plan = await service.PreviewAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(plan.Id, selectedNames: ["../personal.txt"]));
        var result = await service.ExecuteAsync(plan.Id, selectedNames: ["first.tmp"]);
        Assert.Equal(1, result.Deleted); Assert.Equal(7, result.FreedBytes); Assert.Single(result.Items);
        Assert.False(File.Exists(first)); Assert.True(File.Exists(second));
    }
    [Fact]
    public async Task ExactTenMinuteExpiryInvalidatesPreview()
    {
        var path = FileAt("preserve.tmp", clock.GetUtcNow().UtcDateTime.AddDays(-10));
        var service = new TempCleanupService(root, clock); var plan = await service.PreviewAsync(); clock.Now = clock.Now.AddMinutes(10);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(plan.Id)); Assert.True(File.Exists(path));
    }
    [Fact]
    public async Task EntryLimitIsReportedInsteadOfPretendingToScanEverything()
    {
        for (var i = 0; i < 10001; i++) FileAt($"recent-{i}.tmp", clock.GetUtcNow().UtcDateTime);
        var plan = await new TempCleanupService(root, clock).PreviewAsync();
        Assert.True(plan.Analysis!.LimitReached); Assert.True(plan.Analysis.IsPartial); Assert.Equal(10000, plan.Analysis.EntriesExamined);
        Assert.Equal(plan.Analysis.FilesConsidered, plan.Analysis.Eligible + plan.Analysis.Ignored);
    }
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CreateHardLink(string name, string existing, IntPtr security);
}
