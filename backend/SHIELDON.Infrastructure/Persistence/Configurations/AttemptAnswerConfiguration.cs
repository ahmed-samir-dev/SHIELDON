using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PointsAwarded)
            .HasPrecision(5, 2);

        builder.HasOne(e => e.Question)
            .WithMany()
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SelectedOption)
            .WithMany()
            .HasForeignKey(e => e.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
