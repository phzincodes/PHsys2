using PHsystem.Core;
using PHsystem.Diagnostics;
using Xunit;

namespace PHsystem.UnitTests;

public sealed class ScanCoordinatorTests
{
    [Fact]
    public async Task ProgressReportsActualCollectorOutcomesIncludingFailureAndCache()
    {
        var updates = new System.Collections.Concurrent.ConcurrentQueue<CollectorProgress>();
        var store = new SnapshotStore();
        var coordinator = new ScanCoordinator([new Collector("ok", _ => Task.FromResult(Result("ok"))),
            new Collector("bad", _ => throw new IOException())], store, new MemoryRepository());
        var progress = new InlineProgress(updates.Enqueue);
        var snapshot = await coordinator.ScanAsync(progress: progress);
        Assert.Matches(@"^\d+\.\d+\.\d+\+[a-f0-9]{12}", snapshot.CollectorVersion);
        Assert.Equal(2, updates.Count(p => p.Finished));
        Assert.Contains(updates, p => p.Category == "ok" && p.State == "Concluído" && p.Finished);
        Assert.Contains(updates, p => p.Category == "bad" && p.State == "Indisponível" && p.Finished);
        updates.Clear();
        await coordinator.ScanAsync(progress: progress);
        Assert.Equal(2, updates.Count(p => p.Finished));
        Assert.DoesNotContain(updates, p => p.Category == "ok" && p.State == "Analisando");
        store.Set(snapshot with { CollectorVersion="legacy" });updates.Clear();
        await coordinator.ScanAsync(progress:progress);
        Assert.Contains(updates,p=>p.Category=="ok" && p.State=="Analisando");
    }
    private sealed class InlineProgress(Action<CollectorProgress> report) : IProgress<CollectorProgress>
    { public void Report(CollectorProgress value) => report(value); }
    [Fact]
    public async Task FailureInOneCollectorDoesNotDiscardOtherEvidence()
    {
        var repository = new MemoryRepository();
        var store = new SnapshotStore();
        var coordinator = new ScanCoordinator([new Collector("ok", _ => Task.FromResult(Result("ok"))),
            new Collector("bad", _ => throw new IOException("private-path"))], store, repository);
        var snapshot = await coordinator.ScanAsync();
        Assert.Same(snapshot, store.Current);
        Assert.Same(snapshot, repository.Latest);
        Assert.Equal(.5, snapshot.Coverage);
        Assert.Equal(CollectionState.Error, snapshot.Categories[1].State);
        Assert.DoesNotContain("private-path", string.Join("", snapshot.Categories[1].Errors));
        Assert.Null(snapshot.HealthScore);
    }

    [Fact]
    public async Task CancellationPreservesPreviousSnapshotAndDoesNotPersistPartialWork()
    {
        using var source = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new MemoryRepository();
        var store = new SnapshotStore();
        var old = new DiagnosticSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", []);
        store.Set(old);
        var coordinator = new ScanCoordinator([new Collector("slow", async token =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return Result("slow");
        })], store, repository);
        var scan = coordinator.ScanAsync(cancellationToken: source.Token);
        await started.Task;
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scan);
        Assert.Same(old, store.Current);
        Assert.Null(repository.Latest);
    }

    [Fact]
    public async Task TimeoutProducesAnErrorCategory()
    {
        var coordinator = new ScanCoordinator([new Collector("slow", async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Result("slow");
        })], new(), new MemoryRepository(), timeout: TimeSpan.FromMilliseconds(30));
        var snapshot = await coordinator.ScanAsync();
        Assert.Equal(CollectionState.Error, Assert.Single(snapshot.Categories).State);
    }

    [Fact]
    public async Task ConcurrentScansAreRejected()
    {
        var release = new TaskCompletionSource<CategoryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new ScanCoordinator([new Collector("one", _ => release.Task)], new(), new MemoryRepository());
        var first = coordinator.ScanAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ScanAsync());
        release.SetResult(Result("one"));
        await first;
    }

    [Fact]
    public async Task FreshCompleteEvidenceIsReusedUnlessForced()
    {
        var calls = 0;
        var coordinator = new ScanCoordinator([new Collector("one", _ => { calls++; return Task.FromResult(Result("one")); })], new(), new MemoryRepository());
        await coordinator.ScanAsync();
        await coordinator.ScanAsync();
        Assert.Equal(1, calls);
        await coordinator.ScanAsync(force: true);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void DuplicateCategoriesAreRejected() => Assert.Throws<ArgumentException>(() =>
        new ScanCoordinator([new Collector("same", _ => Task.FromResult(Result("same"))), new Collector("same", _ => Task.FromResult(Result("same")))], new(), new MemoryRepository()));

    private static CategoryResult Result(string category) => new(category, CollectionState.Complete, DateTimeOffset.UtcNow.AddMinutes(5), "test", 1, []);

    [Fact]
    public async Task ThreeFailuresOpenCircuitAndCooldownAllowsRetry()
    {
        var calls = 0;
        var clock = new TestClock();
        var coordinator = new ScanCoordinator([new Collector("bad", _ => { calls++; throw new IOException(); })], new(), new MemoryRepository(), clock);
        for (var i = 0; i < 4; i++) await coordinator.ScanAsync(force: true);
        Assert.Equal(3, calls);
        clock.Now = clock.Now.AddMinutes(3);
        await coordinator.ScanAsync(force: true);
        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task CollectorConcurrencyIsBoundedToThree()
    {
        var active = 0; var maximum = 0;
        var collectors = Enumerable.Range(0, 8).Select(i => new Collector("category" + i, async token =>
        {
            var current = Interlocked.Increment(ref active);
            int observed;
            do { observed = Volatile.Read(ref maximum); if (observed >= current) break; }
            while (Interlocked.CompareExchange(ref maximum, current, observed) != observed);
            await Task.Delay(25, token);
            Interlocked.Decrement(ref active);
            return Result("category" + i);
        }));
        var snapshot = await new ScanCoordinator(collectors, new(), new MemoryRepository()).ScanAsync();
        Assert.Equal(3, maximum);
        Assert.Equal(8, snapshot.Categories.Length);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task InvalidConfidenceCannotBePublishedAsEvidence(double confidence)
    {
        var coordinator = new ScanCoordinator([new Collector("invalid", _ => Task.FromResult(Result("invalid") with { Confidence = confidence }))], new(), new MemoryRepository());
        Assert.Equal(CollectionState.Error, Assert.Single((await coordinator.ScanAsync()).Categories).State);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Collector(string category, Func<CancellationToken, Task<CategoryResult>> collect) : IDiagnosticCollector
    {
        public string Category => category;
        public Task<CategoryResult> CollectAsync(CancellationToken cancellationToken) => collect(cancellationToken);
    }
    private sealed class MemoryRepository : ISnapshotRepository
    {
        public DiagnosticSnapshot? Latest { get; private set; }
        public Task SaveAsync(DiagnosticSnapshot snapshot, CancellationToken cancellationToken = default) { Latest = snapshot; return Task.CompletedTask; }
        public Task<DiagnosticSnapshot?> LoadLatestAsync(CancellationToken cancellationToken = default) => Task.FromResult(Latest);
        public Task<IReadOnlyList<DiagnosticSnapshot>> LoadHistoryAsync(int limit = 20, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DiagnosticSnapshot>>(Latest is null ? [] : [Latest]);
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) { Latest = null; return Task.CompletedTask; }
    }
}
