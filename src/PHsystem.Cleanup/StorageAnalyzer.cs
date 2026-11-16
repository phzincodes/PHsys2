namespace PHsystem.Cleanup;

public sealed record StorageFile(string Path, long Bytes, DateTime LastWriteUtc);
public sealed record DuplicateGroup(long BytesPerFile, IReadOnlyList<string> Paths);
public sealed record StorageAnalysis(IReadOnlyList<StorageFile> LargestFiles, IReadOnlyList<DuplicateGroup> Duplicates,
    int FilesObserved, long BytesObserved, int Skipped, int FilesHashed, bool Limited);

/// <summary>Explicitly selected directory only. Read-only, bounded and never stores file lists in the snapshot.</summary>
public sealed class StorageAnalyzer
{
    public Task<StorageAnalysis> AnalyzeAsync(string selectedDirectory, bool compareHashes, CancellationToken token = default) =>
        Task.Run(async () =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Análise segura por handle requer Windows.");
            var root = Path.GetFullPath(selectedDirectory);
            using var rootPin = WindowsDeletionScope.Open(root);
            var files = new List<StorageFile>();
            var pending = new Stack<string>(); pending.Push(root);
            var skipped = 0; var limited = false;
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(60));
            var directories = 0;
            try
            {
                while (pending.Count > 0)
                {
                    budget.Token.ThrowIfCancellationRequested();
                    if (files.Count >= 20000 || ++directories > 5000) { limited = true; break; }
                    var directory = pending.Pop();
                    try
                    {
                        using var pin = WindowsDeletionScope.Open(directory);
                        var entries = 0;
                        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
                        {
                            budget.Token.ThrowIfCancellationRequested();
                            if (++entries > 20000 || files.Count >= 20000 || pending.Count >= 5000) { limited = true; break; }
                            try
                            {
                                var attributes = File.GetAttributes(path);
                                if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0) { skipped++; continue; }
                                if ((attributes & FileAttributes.Directory) != 0) { pending.Push(path); continue; }
                                var info = new FileInfo(path);
                                files.Add(new(path, info.Length, info.LastWriteTimeUtc));
                            }
                            catch (IOException) { skipped++; }
                            catch (UnauthorizedAccessException) { skipped++; }
                        }
                    }
                    catch (IOException) { skipped++; }
                    catch (UnauthorizedAccessException) { skipped++; }
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { limited = true; }
            var hashed = new List<(StorageFile File, string Hash)>();
            long hashedBytes = 0;
            if (compareHashes && !budget.IsCancellationRequested)
                foreach (var file in files.GroupBy(f => f.Bytes).Where(g => g.Count() > 1).SelectMany(g => g))
                {
                    token.ThrowIfCancellationRequested();
                    if (budget.IsCancellationRequested || file.Bytes > 256 * 1024 * 1024 || hashedBytes + file.Bytes > 1024L * 1024 * 1024)
                    { limited = true; skipped++; continue; }
                    try
                    {
                        using var pin = WindowsDeletionScope.Open(Path.GetDirectoryName(file.Path)!);
                        var hash = await pin.HashAsync(new(Path.GetFileName(file.Path), file.Bytes, file.LastWriteUtc), budget.Token);
                        if (hash is null) { skipped++; continue; }
                        hashedBytes += file.Bytes;
                        hashed.Add((file, hash));
                    }
                    catch (IOException) { skipped++; }
                    catch (UnauthorizedAccessException) { skipped++; }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested) { limited = true; break; }
                }
            token.ThrowIfCancellationRequested();
            return new StorageAnalysis(files.OrderByDescending(f => f.Bytes).Take(100).ToArray(),
                hashed.GroupBy(f => (f.File.Bytes, f.Hash)).Where(g => g.Count() > 1)
                    .Select(g => new DuplicateGroup(g.Key.Bytes, g.Select(f => f.File.Path).ToArray())).ToArray(),
                files.Count, files.Sum(f => f.Bytes), skipped, hashed.Count, limited);
        }, token);
}
