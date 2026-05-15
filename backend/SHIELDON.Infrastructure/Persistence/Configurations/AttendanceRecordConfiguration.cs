using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(r => r.Id);

        // ── Relationships ──────────────────────────────────────
        builder.HasOne(r => r.AttendanceCheck)
            .WithMany(a => a.Records)
            .HasForeignKey(r => r.AttendanceCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Unique Constraint: one record per (check, student) ─
        builder.HasIndex(r => new { r.AttendanceCheckId, r.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceRecords_Check_Student");
    }
}
