using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PHsystem.Server;

public sealed class CommunityDbFactory : IDesignTimeDbContextFactory<CommunityDb>
{
    public CommunityDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CommunityDb>()
        .UseNpgsql("Host=127.0.0.1;Database=phsystem_design").Options);
}
