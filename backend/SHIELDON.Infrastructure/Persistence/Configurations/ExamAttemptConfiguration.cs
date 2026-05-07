using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Score)
            .HasPrecision(5, 2);

        // Phase 5: tracks the last heartbeat timestamp for disconnection detection
        builder.Property(e => e.LastHeartbeatAt)
            .HasColumnType("DATETIME2");

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Answers)
            .WithOne(a => a.Attempt)
            .HasForeignKey(a => a.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Token)
            .WithOne(t => t.Attempt)
            .HasForeignKey<ExamToken>(t => t.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Phase 5 navigation: PresenceLogs and ReviewDecision are configured
        // in their own configuration files (PresenceLogConfiguration, ReviewDecisionConfiguration)
    }
}
