using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("PaymentRecords");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.AmountUSD)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.StripeSessionId)
            .HasMaxLength(200);

        builder.Property(e => e.PaidAt)
            .HasColumnType("DATETIME2");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: Student ─────────────────────────────────────────
        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: Course ──────────────────────────────────────────
        builder.HasOne(e => e.Course)
            .WithMany()
            .HasForeignKey(e => e.CourseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: Enrollment ──────────────────────────────────────
        builder.HasOne(e => e.Enrollment)
            .WithMany()
            .HasForeignKey(e => e.EnrollmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade); // If enrollment is deleted, delete the payment record
    }
}
