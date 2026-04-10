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
            // 1. Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();

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
