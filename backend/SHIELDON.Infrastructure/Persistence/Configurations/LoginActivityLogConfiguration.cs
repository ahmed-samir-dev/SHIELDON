using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class LoginActivityLogConfiguration : IEntityTypeConfiguration<LoginActivityLog>
{
    public void Configure(EntityTypeBuilder<LoginActivityLog> builder)
    {
        builder.ToTable("LoginActivityLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.IsSuccess).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
