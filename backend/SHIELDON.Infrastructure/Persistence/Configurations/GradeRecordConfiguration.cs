using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class GradeRecordConfiguration : IEntityTypeConfiguration<GradeRecord>
{
    public void Configure(EntityTypeBuilder<GradeRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Score)
            .HasPrecision(5, 2);

        builder.Property(e => e.MaxScore)
            .HasPrecision(5, 2);

        builder.Property(e => e.Weight)
            .HasPrecision(5, 2);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
            .WithMany()
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Exam)
            .WithMany(e => e.GradeRecords)
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Assignment)
            .WithMany(a => a.GradeRecords)
            .HasForeignKey(e => e.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
