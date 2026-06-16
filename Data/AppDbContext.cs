using Microsoft.EntityFrameworkCore;

namespace rytmix_api.Data;

/// <summary>
/// EF Core database context for Rytmix. Wired to PostgreSQL via Npgsql in
/// <c>Program.cs</c> (the connection string comes from configuration, never
/// hardcoded).
///
/// This is the Phase 0 skeleton, so it intentionally holds no entity sets yet.
/// The entities (User, Playlist, Track, Favorite) and their <see cref="DbSet{TEntity}"/>
/// properties — plus the first EF Core migration — are added in Phase 2.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet<...> properties will be added in Phase 2 (accounts + persistence).
}
