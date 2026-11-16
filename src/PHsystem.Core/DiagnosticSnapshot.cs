using System.Collections.Immutable;
namespace PHsystem.Core;
public enum CollectionState { Unavailable, Partial, Complete, Error }
public sealed record CategoryResult(string Category, CollectionState State, DateTimeOffset ExpiresAt, string Source, double Confidence, ImmutableArray<string> Errors,
    ImmutableDictionary<string, string>? Facts = null);
public sealed record DiagnosticSnapshot(Guid ScanId, DateTimeOffset CapturedAt, string CollectorVersion, ImmutableArray<CategoryResult> Categories)
{
    public double Coverage => Categories.IsEmpty ? 0 : (double)Categories.Count(c => c.State == CollectionState.Complete) / Categories.Length;
    public int? HealthScore => HealthAssessmentEngine.Evaluate(this).Score;
}
public interface ISnapshotStore { DiagnosticSnapshot? Current { get; } }
