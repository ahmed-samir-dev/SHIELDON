using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamTokenConfiguration : IEntityTypeConfiguration<ExamToken>
{
    public void Configure(EntityTypeBuilder<ExamToken> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Token)
            .IsUnique();
    }
}
