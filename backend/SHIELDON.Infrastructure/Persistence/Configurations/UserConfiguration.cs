using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core Fluent API configuration for the User entity.
/// Defines constraints, indexes, and column mappings enforced at the database level.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // ── Identity ───────────────────────────────────────────
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Unique constraint on email - prevents duplicate registrations
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        // ── Profile ───────────────────────────────────────────
        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(512);

        // ── Role & Status ─────────────────────────────────────
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()        // Store enum name as string for readability
            .HasMaxLength(50);

        builder.Property(u => u.AccountStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // ── Security token fields ─────────────────────────────
        builder.Property(u => u.VerificationCode)
            .HasMaxLength(512); // URL-safe base64 token (~64 chars)

        builder.Property(u => u.VerificationCodeExpiresAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.ResetPasswordCode)
            .HasMaxLength(512);

        // ── Timestamps (UTC) ──────────────────────────────────
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.ResetPasswordCodeExpiresAt)
            .HasColumnType("datetime2");

        // ── Navigation - Tokens ───────────────────────────────
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Index for faster login lookups ────────────────────
        builder.HasIndex(u => u.AccountStatus)
            .HasDatabaseName("IX_Users_AccountStatus");
    }
}
