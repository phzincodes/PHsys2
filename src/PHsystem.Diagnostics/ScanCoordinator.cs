using System.Collections.Immutable;
using System.Collections.Concurrent;
using PHsystem.Core;

namespace PHsystem.Diagnostics;

public sealed record CollectorProgress(string Category, string State, bool Finished);

public sealed class ScanCoordinator
{
    private readonly IDiagnosticCollector[] collectors;
    private readonly SnapshotStore store;
    private readonly ISnapshotRepository repository;
    private readonly TimeProvider time;
    private readonly TimeSpan timeout;
    private readonly string collectorVersion;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConcurrentDictionary<string, (int Failures, DateTimeOffset RetryAt)> failures = new();

    public ScanCoordinator(IEnumerable<IDiagnosticCollector> collectors, SnapshotStore store,
        ISnapshotRepository repository, TimeProvider? time = null, TimeSpan? timeout = null)
    {
        this.collectors = collectors.ToArray();
        if (this.collectors.Length is 0 or > 32 || this.collectors.Select(c => c.Category).Distinct(StringComparer.Ordinal).Count() != this.collectors.Length)
            throw new ArgumentException("Coletores devem ter categorias únicas e não vazias.", nameof(collectors));
        if (this.collectors.Any(c => string.IsNullOrWhiteSpace(c.Category)))
            throw new ArgumentException("Categoria vazia.", nameof(collectors));
        collectorVersion = string.Join(";", this.collectors.Select(c => BuildIdentity.From(c.GetType().Assembly)).Select(b => b.Version + "+" + b.BuildId).Distinct().OrderBy(v => v, StringComparer.Ordinal));
        this.store = store;
        this.repository = repository;
        this.time = time ?? TimeProvider.System;
        this.timeout = timeout ?? TimeSpan.FromSeconds(20);
        if (this.timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    public IReadOnlyList<string> Categories => Array.AsReadOnly(collectors.Select(c => c.Category).ToArray());

    public async Task<DiagnosticSnapshot> ScanAsync(bool force = false, CancellationToken cancellationToken = default, IProgress<CollectorProgress>? progress = null)
    {
        if (!await gate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Já existe uma varredura em andamento.");
        try
        {
            var now = time.GetUtcNow();
            var previous = store.Current;
            using var parallelism = new SemaphoreSlim(3, 3);
            async Task<CategoryResult> CollectOneAsync(IDiagnosticCollector collector)
            {
                var cached = previous?.Categories.FirstOrDefault(c => c.Category == collector.Category);
                if (!force && previous?.CollectorVersion == collectorVersion && cached is not null && cached.ExpiresAt > now && cached.State == CollectionState.Complete)
                    return cached;
                if (failures.TryGetValue(collector.Category, out var circuit) && circuit.Failures >= 3 && circuit.RetryAt > now)
                    return Failure(collector.Category, "Coletor temporariamente suspenso após falhas repetidas. Tente novamente em 2 minutos.");
                await parallelism.WaitAsync(cancellationToken);
                progress?.Report(new(collector.Category, "Analisando", false));
                using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                source.CancelAfter(timeout);
                try
                {
                    var result = await collector.CollectAsync(source.Token).WaitAsync(source.Token);
                    if (result.Category != collector.Category || !double.IsFinite(result.Confidence)
                        || result.Confidence is < 0 or > 1 || result.Errors.IsDefault || result.Errors.Length > 32
                        || result.Facts?.Count > 256 || result.Facts?.Any(f => f.Key.Length > 512 || f.Value.Length > 2048) == true)
                        throw new InvalidDataException("Resultado inválido do coletor.");
                    failures.TryRemove(collector.Category, out _);
                    return result;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    RecordFailure(collector.Category);
                    return Failure(collector.Category, "Tempo limite da coleta excedido.");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Do not persist raw exception messages: they can contain private paths or identifiers.
                    RecordFailure(collector.Category);
                    return Failure(collector.Category, $"Coleta indisponível ({ex.GetType().Name}).");
                }
                finally { parallelism.Release(); }
            }
            var tasks = collectors.Select(async collector =>
            {
                progress?.Report(new(collector.Category, "Na fila", false));
                var result = await CollectOneAsync(collector);
                var state = result.State switch { CollectionState.Complete => "Concluído", CollectionState.Partial => "Parcial", CollectionState.Error => "Indisponível", _ => "Sem dados" };
                progress?.Report(new(collector.Category, state, true));
                return result;
            });
            var categories = await Task.WhenAll(tasks);
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = new DiagnosticSnapshot(Guid.NewGuid(), now, collectorVersion, categories.ToImmutableArray());
            await repository.SaveAsync(snapshot, cancellationToken);
            store.Set(snapshot);
            return snapshot;
        }
        finally { gate.Release(); }
    }

    private CategoryResult Failure(string category, string error) => new(category, CollectionState.Error,
        time.GetUtcNow(), "coordinator", 0, [error]);

    private void RecordFailure(string category) => failures.AddOrUpdate(category,
        _ => (1, time.GetUtcNow().AddMinutes(2)), (_, value) => (value.Failures + 1, time.GetUtcNow().AddMinutes(2)));
}
