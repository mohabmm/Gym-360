using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class TrainerRatingConfiguration : IEntityTypeConfiguration<TrainerRating>
{
    public void Configure(EntityTypeBuilder<TrainerRating> builder)
    {
        builder.ToTable("trainer_ratings");

        builder.HasKey(tr => tr.Id);

        builder.Property(tr => tr.Id)
            .ValueGeneratedNever();

        builder.Property(tr => tr.GymId)
            .IsRequired();

        builder.Property(tr => tr.TrainerId)
            .IsRequired();

        builder.Property(tr => tr.ClientId)
            .IsRequired();

        builder.Property(tr => tr.SessionId)
            .IsRequired();

        builder.Property(tr => tr.Stars)
            .IsRequired();

        builder.Property(tr => tr.Comment)
            .HasMaxLength(1000);

        builder.Property(tr => tr.CreatedAt)
            .IsRequired();

        builder.HasIndex(tr => new { tr.TrainerId, tr.GymId });
        builder.HasIndex(tr => tr.SessionId);
    }
}
