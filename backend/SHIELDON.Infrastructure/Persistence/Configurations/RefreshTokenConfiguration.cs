using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core Fluent API configuration for the RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        // ── Token ─────────────────────────────────────────────
        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);

        // Index for fast token lookup on refresh endpoint
        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // ── Revocation ────────────────────────────────────────
        builder.Property(t => t.RevokedReason)
            .HasMaxLength(200);

        // ── Timestamps (UTC) ──────────────────────────────────
        builder.Property(t => t.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(t => t.RevokedAt)
            .HasColumnType("datetime2");

        // ── Index for cleanup jobs ────────────────────────────
        // Allows efficient querying of expired tokens for purging
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
    }
}
