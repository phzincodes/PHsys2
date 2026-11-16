using System.Collections.Immutable;

namespace PHsystem.Cleanup;

public enum CleanupSkipReason { Recent, Protected, InUse, PermissionDenied, ReparsePoint, HardLink, OutsidePolicy, ReadError }
public sealed record CleanupAnalysis(int EntriesExamined, int DirectoriesExamined, int DirectoriesExcluded, int DirectoryLinksExcluded,
    int FilesConsidered, int Eligible, ImmutableDictionary<CleanupSkipReason, int> IgnoredByReason, bool LimitReached, int EnumerationErrors,
    int UnidentifiedEntries = 0, int EntryPermissionErrors = 0, int EntryReadErrors = 0)
{
    public int Ignored => IgnoredByReason.Values.Sum();
    public bool IsPartial => LimitReached || EnumerationErrors > 0 || UnidentifiedEntries > 0 || Count(CleanupSkipReason.PermissionDenied) > 0 || Count(CleanupSkipReason.ReadError) > 0;
    public int Count(CleanupSkipReason reason) => IgnoredByReason.GetValueOrDefault(reason);
}
public sealed record CleanupFileResult(string Name, bool Removed, string Explanation);
internal sealed record CleanupProbe(CleanupCandidate Candidate, CleanupSkipReason? Reason);
