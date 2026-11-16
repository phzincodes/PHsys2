using Microsoft.EntityFrameworkCore;
using Npgsql;
using PHsystem.Server;
using Xunit;

namespace PHsystem.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHSYSTEM_TEST_POSTGRES")))
            Skip = "PostgreSQL de teste não configurado. Defina PHSYSTEM_TEST_POSTGRES para um banco exclusivo phsystem_test_*.";
    }
}

public sealed class PostgresTests
{
    [PostgresFact]
    [Trait("Database", "PostgreSQL")]
    public async Task ConcurrentReactionsAreIdempotentAndPersistAcrossConnections()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PHSYSTEM_TEST_POSTGRES"));
        Assert.StartsWith("phsystem_test_", connection.Database);
        var options = new DbContextOptionsBuilder<CommunityDb>().UseNpgsql(connection.ConnectionString).Options;
        await using var seed = new CommunityDb(options); await seed.Database.MigrateAsync();
        var author = Guid.NewGuid().ToString("N"); var reader = Guid.NewGuid().ToString("N");
        var post = new ForumPost { AuthorId = author, Title = "Isolated concurrency fixture", Body = "Not production content", Category = "hardware" };
        seed.Users.AddRange(new CommunityUser { Id = author, UserName = "author_" + author, NormalizedUserName = "AUTHOR_" + author },
            new CommunityUser { Id = reader, UserName = "reader_" + reader, NormalizedUserName = "READER_" + reader });
        seed.Posts.Add(post); await seed.SaveChangesAsync();
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ => { await using var db = new CommunityDb(options); Assert.Equal(1, await PostVoteStore.SetAsync(db, post.Id, reader, 1)); }));
            await using var verify = new CommunityDb(options);
            Assert.Equal(1, await verify.PostVotes.CountAsync(v => v.PostId == post.Id));
            Assert.Equal(1, await verify.PostVotes.Where(v => v.PostId == post.Id).SumAsync(v => v.Value));
            Assert.Equal(0, await PostVoteStore.SetAsync(verify, post.Id, author, 1));
            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ => { await using var db = new CommunityDb(options); await PostVoteStore.SetAsync(db, post.Id, reader, 0); }));
            Assert.False(await verify.PostVotes.AnyAsync(v => v.PostId == post.Id));
            verify.Blocks.Add(new() { OwnerId = author, BlockedId = reader }); await verify.SaveChangesAsync();
            Assert.Equal(0, await PostVoteStore.SetAsync(verify, post.Id, reader, 1));
            await verify.Blocks.Where(b => b.OwnerId == author).ExecuteDeleteAsync();
            post.Hidden = true; await seed.SaveChangesAsync();
            Assert.Equal(0, await PostVoteStore.SetAsync(verify, post.Id, reader, 1));
        }
        finally
        {
            await seed.Blocks.Where(b => b.OwnerId == author || b.BlockedId == reader).ExecuteDeleteAsync();
            await seed.Posts.Where(p => p.Id == post.Id).ExecuteDeleteAsync();
            await seed.Users.Where(u => u.Id == author || u.Id == reader).ExecuteDeleteAsync();
        }
    }
    [PostgresFact]
    [Trait("Database", "PostgreSQL")]
    public async Task RealPostgresAppliesMigrationsAndEnforcesIdentityUniqueness()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PHSYSTEM_TEST_POSTGRES"));
        Assert.StartsWith("phsystem_test_", connection.Database);
        var options = new DbContextOptionsBuilder<CommunityDb>().UseNpgsql(connection.ConnectionString).Options;
        await using var db = new CommunityDb(options);
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var suffix = Guid.NewGuid().ToString("N");
        var first = new CommunityUser { Id = suffix, UserName = "fixture_" + suffix, NormalizedUserName = "FIXTURE_" + suffix,
            Email = suffix + "@example.invalid", NormalizedEmail = suffix.ToUpperInvariant() + "@EXAMPLE.INVALID" };
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Users.Add(first);
        await db.SaveChangesAsync();
        db.Posts.Add(new() { AuthorId = first.Id, Title = "Fixture de integração", Body = "Conteúdo isolado de teste", Category = "hardware" });
        await db.SaveChangesAsync();
        db.Users.Add(new() { Id = Guid.NewGuid().ToString(), UserName = first.UserName!.ToLowerInvariant(), NormalizedUserName = first.NormalizedUserName });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await transaction.RollbackAsync();
    }
}
