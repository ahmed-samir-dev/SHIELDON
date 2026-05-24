using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamExtensionConfiguration : IEntityTypeConfiguration<ExamExtension>
{
    public void Configure(EntityTypeBuilder<ExamExtension> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Exam)
            .WithMany()
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SourceRequest)
            .WithMany()
            .HasForeignKey(e => e.SourceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one extension per student per exam
        builder.HasIndex(e => new { e.StudentId, e.ExamId }).IsUnique();
    }
}
