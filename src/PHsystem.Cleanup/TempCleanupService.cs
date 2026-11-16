using System.Collections.Immutable;

namespace PHsystem.Cleanup;

public sealed record CleanupCandidate(string Name, long Bytes, DateTime LastWriteUtc)
{
    internal ulong FileId { get; init; }
    internal bool HasFileId { get; init; }
}
public sealed record CleanupPlan(Guid Id, DateTimeOffset CreatedAt, ImmutableArray<CleanupCandidate> Files, int Skipped, CleanupAnalysis? Analysis = null)
{
    public long TotalBytes => Files.Sum(f => f.Bytes);
}
// FreedBytes is the legacy name for nominal bytes of confirmed deletions, not physical free space.
public sealed record CleanupOutcome(int Deleted, int Skipped, long FreedBytes, long? ObservedFreeSpaceChange = null,
    ImmutableArray<CleanupFileResult> Items = default, bool Cancelled = false);

/// <summary>Only old top-level files in a fixed temporary directory. No recursive deletion.</summary>
public sealed class TempCleanupService
{
    private readonly string root;
    private readonly TimeProvider time;
    private readonly SemaphoreSlim gate = new(1, 1);
    private CleanupPlan? lastPlan;
    public string ScanRoot => root;

    public TempCleanupService(string temporaryDirectory, TimeProvider? time = null)
    {
        root = Path.GetFullPath(temporaryDirectory);
        if (Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root)!) == Path.TrimEndingDirectorySeparator(root))
            throw new ArgumentException("A raiz de um volume não é uma pasta temporária.", nameof(temporaryDirectory));
        this.time = time ?? TimeProvider.System;
    }

    public async Task<CleanupPlan> PreviewAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return lastPlan = await Task.Run(() =>
            {
                EnsureNoLinks(root);
                if (OperatingSystem.IsWindows())
                { using var scope = WindowsDeletionScope.Open(root); return AnalyzeEntries(cancellationToken, scope.Probe); }
                return AnalyzeEntries(cancellationToken, candidate => new(candidate, null));
            }, cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task<CleanupOutcome> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default, IReadOnlyCollection<string>? selectedNames = null)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var plan = lastPlan;
            if (plan is null || plan.Id != planId || time.GetUtcNow() - plan.CreatedAt >= TimeSpan.FromMinutes(10))
                throw new InvalidOperationException("Gere uma nova prévia antes de limpar.");
            if (selectedNames is not null && (selectedNames.Count == 0 || selectedNames.Count != selectedNames.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                || selectedNames.Any(name => !plan.Files.Any(file => string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase)))))
                throw new InvalidOperationException("A seleção não corresponde à prévia autorizada.");
            lastPlan = null; // A plan is single-use, even on cancellation or failure.
            return await Task.Run(() =>
            {
                EnsureNoLinks(root);
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Limpeza por handle requer Windows.");
                using var scope = WindowsDeletionScope.Open(root);
                var deleted = 0;
                var skipped = 0;
                long freed = 0;
                var before = FreeSpace();
                var items = ImmutableArray.CreateBuilder<CleanupFileResult>();
                foreach (var candidate in plan.Files.Where(f => selectedNames is null || selectedNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    if (cancellationToken.IsCancellationRequested) { skipped++; items.Add(new(candidate.Name, false, "Não processado após cancelamento.")); continue; }
                    try
                    {
                        if (!scope.Delete(candidate)) { skipped++; items.Add(new(candidate.Name, false, "Preservado: mudou, está em uso, protegido ou fora da política.")); continue; }
                        deleted++;
                        freed += candidate.Bytes;
                        items.Add(new(candidate.Name, true, "Exclusão confirmada por handle."));
                    }
                    catch (IOException) { skipped++; items.Add(new(candidate.Name, false, "Preservado por falha de acesso ou validação.")); }
                    catch (UnauthorizedAccessException) { skipped++; items.Add(new(candidate.Name, false, "Preservado: permissão insuficiente.")); }
                }
                var after = FreeSpace();
                return new CleanupOutcome(deleted, skipped, freed, before.HasValue && after.HasValue ? after - before : null, items.ToImmutable(), cancellationToken.IsCancellationRequested);
            }, cancellationToken);
        }
        finally { gate.Release(); }
    }

    private CleanupPlan AnalyzeEntries(CancellationToken token, Func<CleanupCandidate, CleanupProbe> probe)
    {
        var now = time.GetUtcNow(); var cutoff = now.UtcDateTime.AddDays(-7);
        var files = ImmutableArray.CreateBuilder<CleanupCandidate>();
        var ignored = new Dictionary<CleanupSkipReason, int>();
        var entries = 0; var directories = 0; var directoryLinks = 0; var considered = 0; var errors = 0; var limited = false;
        var unknown = 0; var entryPermissions = 0; var entryErrors = 0;
        void Skip(CleanupSkipReason reason) => ignored[reason] = ignored.GetValueOrDefault(reason) + 1;
        try
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                if (entries == 10000) { limited = true; break; }
                entries++;
                var identifiedFile = false;
                try
                {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.Directory) != 0)
                    { directories++; if ((attributes & FileAttributes.ReparsePoint) != 0) directoryLinks++; continue; }
                    considered++; identifiedFile = true;
                    if ((attributes & FileAttributes.ReparsePoint) != 0) { Skip(CleanupSkipReason.ReparsePoint); continue; }
                    if ((attributes & (FileAttributes.ReadOnly | FileAttributes.System)) != 0) { Skip(CleanupSkipReason.Protected); continue; }
                    var info = new FileInfo(path);
                    if (info.LastWriteTimeUtc >= cutoff) { Skip(CleanupSkipReason.Recent); continue; }
                    var result = probe(new(info.Name, info.Length, info.LastWriteTimeUtc));
                    if (result.Reason is { } reason) Skip(reason); else files.Add(result.Candidate);
                }
                catch (UnauthorizedAccessException) { if (identifiedFile) Skip(CleanupSkipReason.PermissionDenied); else { unknown++; entryPermissions++; } }
                catch (IOException) { if (identifiedFile) Skip(CleanupSkipReason.ReadError); else { unknown++; entryErrors++; } }
            }
        }
        catch (UnauthorizedAccessException) { errors++; }
        catch (IOException) { errors++; }
        var analysis = new CleanupAnalysis(entries, 1, directories, directoryLinks, considered, files.Count, ignored.ToImmutableDictionary(), limited, errors, unknown, entryPermissions, entryErrors);
        return new(Guid.NewGuid(), now, files.ToImmutable(), analysis.Ignored, analysis);
    }
    private long? FreeSpace()
    {
        try { return new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }

    private static void EnsureNoLinks(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Links não são aceitos na limpeza de temporários.");
    }
}
