using SHIELDON.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SHIELDON.Infrastructure.Persistence;

/// <summary>
/// The main Entity Framework Core database context for SHIELDON.
/// All entities are registered here. All database access flows through this class.
///
/// Convention: PascalCase table and column names (configured per entity).
/// All timestamps stored in UTC (DATETIME2 in SQL Server).
/// All primary keys are GUID (UNIQUEIDENTIFIER).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Phase 1: Auth ──────────────────────────────────────────────
    // DbSets will be added in Stage 1.1 when entities are defined
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes found in this assembly
        // Each entity has its own configuration file in Persistence/Configurations/
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
