using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class LeaderboardRankSnapshotConfiguration : IEntityTypeConfiguration<LeaderboardRankSnapshot>
{
    public void Configure(EntityTypeBuilder<LeaderboardRankSnapshot> builder)
    {
        builder.HasKey(e => e.Id);

        // Composite unique index: one snapshot per (course + student)
        builder.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();

        builder.Property(e => e.Score)
            .HasPrecision(8, 4);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.LeaderboardRankSnapshots)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
