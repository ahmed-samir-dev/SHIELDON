using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for <see cref="CourseDeleteAuditLog"/>.
/// Defines table name, column constraints, FK relationship to Users, and indexes.
///
/// Note: There is intentionally NO foreign key constraint to the Courses table.
/// The CourseId, CourseCode, and CourseTitle columns are value snapshots only —
/// the Course row is permanently destroyed after a hard delete.
/// </summary>
public class CourseDeleteAuditLogConfiguration : IEntityTypeConfiguration<CourseDeleteAuditLog>
{
    public void Configure(EntityTypeBuilder<CourseDeleteAuditLog> builder)
    {
        builder.ToTable("CourseDeleteAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdminFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CourseCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.CourseTitle)
            .IsRequired()
            .HasMaxLength(300);

        // FK to Users (the admin who deleted the course).
        // Uses Restrict so the admin account cannot be deleted while audit records exist.
        builder.HasOne(x => x.DeletedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.DeletedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for querying audit history by admin
        builder.HasIndex(x => x.DeletedByAdminId);

        // Index for date-range queries on the admin audit trail page
        builder.HasIndex(x => x.DeletedAt);
    }
}
