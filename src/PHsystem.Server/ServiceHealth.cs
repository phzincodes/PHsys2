using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using PHsystem.SharedContracts.V1;
using PHsystem.Core;

namespace PHsystem.Server;

public static class ServiceHealth
{
    public static void MapServiceHealth(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { service = "PHsystem.Next", protocolVersion = 1 }));
        app.MapGet("/health", async (CommunityDb db, IAccountEmail email, IConfiguration configuration, IHostEnvironment environment, HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var databaseReady = false;
            var migrationState = "unavailable";
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Community")))
                {
                    // Read-only schema probes. No usernames, counts, connection strings or exceptions leave this endpoint.
                    await db.Users.AsNoTracking().AnyAsync(deadline.Token);
                    await db.Posts.AsNoTracking().AnyAsync(deadline.Token);
                    databaseReady = environment.IsEnvironment("Testing") || !(await db.Database.GetPendingMigrationsAsync(deadline.Token)).Any();
                    migrationState = databaseReady ? "current" : "pending";
                }
            }
            catch (Exception ex) when (ex is DbException or InvalidOperationException or OperationCanceledException or TimeoutException) { databaseReady = false; }
            var registration = databaseReady && email.Configured && CompromisedPasswordValidator.IsConfigured(configuration);
            var recovery = registration;
            var build = BuildIdentity.From(typeof(ServiceHealth).Assembly);
            var health = new ApiHealthDto("PHsystem.Next", ApiProtocol.Version, !databaseReady ? "unavailable" : registration ? "ready" : "degraded", databaseReady, registration, recovery,
                build.Version, build.BuildId, migrationState);
            // Configured SMTP does not prove delivery; monitor the actual provider separately.
            return Results.Json(health, statusCode: databaseReady ? 200 : 503);
        });
    }
}
