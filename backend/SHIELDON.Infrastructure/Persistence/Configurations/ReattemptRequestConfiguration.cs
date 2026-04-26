using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ReattemptRequestConfiguration : IEntityTypeConfiguration<ReattemptRequest>
{
    public void Configure(EntityTypeBuilder<ReattemptRequest> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Justification)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Status)
            .HasMaxLength(50);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Exam)
            .WithMany()
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReviewedBy)
            .WithMany()
            .HasForeignKey(e => e.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
