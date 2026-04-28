using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ResultVisibility)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PassScore)
            .HasPrecision(5, 2);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Exams)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(e => e.SelectionRules)
            .WithOne(r => r.Exam)
            .HasForeignKey(r => r.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.Attempts)
            .WithOne(a => a.Exam)
            .HasForeignKey(a => a.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.GradeRecords)
            .WithOne(g => g.Exam)
            .HasForeignKey(g => g.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
