using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace SHIELDON.Infrastructure.Persistence;

/// <summary>
/// Handles database initialization and seeding of default data (e.g. initial Admin).
/// </summary>
public static class DbInitializer
{
    public static async Task InitAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // 1. Apply migrations for relational DBs (SQL Server, etc.)
            //    For InMemory (integration tests), use EnsureCreated() instead.
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();

                // Ensure soft delete columns exist on SQL Server (W5)
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CourseMaterials]') AND name = 'IsDeleted')
                        ALTER TABLE [CourseMaterials] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CourseMaterials]') AND name = 'DeletedAt')
                        ALTER TABLE [CourseMaterials] ADD [DeletedAt] datetime2 NULL;

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Announcements]') AND name = 'IsDeleted')
                        ALTER TABLE [Announcements] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Announcements]') AND name = 'DeletedAt')
                        ALTER TABLE [Announcements] ADD [DeletedAt] datetime2 NULL;

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ExamQuestions]') AND name = 'IsDeleted')
                        ALTER TABLE [ExamQuestions] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ExamQuestions]') AND name = 'DeletedAt')
                        ALTER TABLE [ExamQuestions] ADD [DeletedAt] datetime2 NULL;

                    -- Expand UserActivityLogs schema
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'Category')
                        ALTER TABLE [UserActivityLogs] ADD [Category] nvarchar(50) NOT NULL DEFAULT 'SYSTEM';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'Action')
                        ALTER TABLE [UserActivityLogs] ADD [Action] nvarchar(100) NOT NULL DEFAULT '';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'UserEmail')
                        ALTER TABLE [UserActivityLogs] ADD [UserEmail] nvarchar(150) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'UserRole')
                        ALTER TABLE [UserActivityLogs] ADD [UserRole] nvarchar(50) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'Description')
                        ALTER TABLE [UserActivityLogs] ADD [Description] nvarchar(1000) NOT NULL DEFAULT '';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'EntityId')
                        ALTER TABLE [UserActivityLogs] ADD [EntityId] nvarchar(100) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'EntityType')
                        ALTER TABLE [UserActivityLogs] ADD [EntityType] nvarchar(100) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'MetadataJson')
                        ALTER TABLE [UserActivityLogs] ADD [MetadataJson] nvarchar(max) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[UserActivityLogs]') AND name = 'UserAgent')
                        ALTER TABLE [UserActivityLogs] ADD [UserAgent] nvarchar(500) NULL;

                    ALTER TABLE [UserActivityLogs] ALTER COLUMN [UserId] uniqueidentifier NULL;
                ");
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 2. Seed Admin User if not exists
            await SeedAdminAsync(context, configuration, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    private static async Task SeedAdminAsync(AppDbContext context, IConfiguration configuration, ILogger logger)
    {
        var adminSettings = configuration.GetSection("AdminSeed");
        var adminEmail = adminSettings["Email"] ?? "admin@shieldon.com";

        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            return; // Admin already exists
        }

        logger.LogInformation("Seeding initial Admin user: {Email}", adminEmail);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FirstName = adminSettings["FirstName"] ?? "System",
            LastName = adminSettings["LastName"] ?? "Administrator",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminSettings["Password"] ?? "Admin@Shieldon2025!"),
            Role = UserRole.Admin,
            AccountStatus = AccountStatus.Unverified, // Seed as Unverified so we can test the verification flow
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Initial Admin user created as Unverified.");
    }
}
