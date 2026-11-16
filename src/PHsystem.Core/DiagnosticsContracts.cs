namespace PHsystem.Core;

public interface IDiagnosticCollector
{
    string Category { get; }
    Task<CategoryResult> CollectAsync(CancellationToken cancellationToken);
}

public interface ISnapshotRepository
{
    Task SaveAsync(DiagnosticSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<DiagnosticSnapshot?> LoadLatestAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiagnosticSnapshot>> LoadHistoryAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
