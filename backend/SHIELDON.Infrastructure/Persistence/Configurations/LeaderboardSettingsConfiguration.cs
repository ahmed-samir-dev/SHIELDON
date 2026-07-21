using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class LeaderboardSettingsConfiguration : IEntityTypeConfiguration<LeaderboardSettings>
{
    public void Configure(EntityTypeBuilder<LeaderboardSettings> builder)
    {
        builder.HasKey(e => e.Id);

        // Unique: exactly one settings row per course
        builder.HasIndex(e => e.CourseId).IsUnique();

        builder.Property(e => e.ScoringMetric)
            .HasConversion<string>()
            .HasMaxLength(50);

        // 1-to-1 with Course (settings owned by course side)
        builder.HasOne(e => e.Course)
            .WithOne(c => c.LeaderboardSettings)
            .HasForeignKey<LeaderboardSettings>(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // delete settings when course is deleted
    }
}
