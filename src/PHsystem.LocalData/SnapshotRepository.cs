using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHsystem.Core;

namespace PHsystem.LocalData;

public sealed class SnapshotRepository(string databasePath) : ISnapshotRepository
{
    private readonly string databasePath = Path.GetFullPath(databasePath);
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 32 };

    public async Task SaveAsync(DiagnosticSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = await OpenAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.Snapshots.Add(new SnapshotRow { Id = snapshot.ScanId, CapturedAt = snapshot.CapturedAt.UtcTicks,
                Json = JsonSerializer.Serialize(snapshot, JsonOptions) });
            await db.SaveChangesAsync(cancellationToken);
            var expired = await db.Snapshots.OrderByDescending(s => s.CapturedAt).ThenByDescending(s => s.Id)
                .Skip(100).ToListAsync(cancellationToken);
            db.Snapshots.RemoveRange(expired);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task<DiagnosticSnapshot?> LoadLatestAsync(CancellationToken cancellationToken = default)
        => (await LoadHistoryAsync(1, cancellationToken)).FirstOrDefault();

    public async Task<IReadOnlyList<DiagnosticSnapshot>> LoadHistoryAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = await OpenAsync(cancellationToken);
            var rows = await db.Snapshots.AsNoTracking().OrderByDescending(s => s.CapturedAt).ThenByDescending(s => s.Id)
                .Take(limit).ToListAsync(cancellationToken);
            return rows.Select(row => JsonSerializer.Deserialize<DiagnosticSnapshot>(row.Json, JsonOptions)
                ?? throw new InvalidDataException("Snapshot local inválido.")).ToArray();
        }
        finally { gate.Release(); }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = await OpenAsync(cancellationToken);
            await db.Snapshots.ExecuteDeleteAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
        }
        finally { gate.Release(); }
    }

    private async Task<SnapshotDb> OpenAsync(CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var connection = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = databasePath, Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        var db = new SnapshotDb(new DbContextOptionsBuilder<SnapshotDb>().UseSqlite(connection.ToString()).Options);
        try
        {
            await db.Database.MigrateAsync(token);
            return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }
}

internal sealed class SnapshotDb(DbContextOptions<SnapshotDb> options) : DbContext(options)
{
    public DbSet<SnapshotRow> Snapshots => Set<SnapshotRow>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SnapshotRow>().HasKey(s => s.Id);
        modelBuilder.Entity<SnapshotRow>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<SnapshotRow>().HasIndex(s => s.CapturedAt);
    }
}

internal sealed class SnapshotRow
{
    public Guid Id { get; set; }
    public long CapturedAt { get; set; }
    public string Json { get; set; } = "";
}
