using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class TrainerPublicProfileConfiguration : IEntityTypeConfiguration<TrainerPublicProfile>
{
    public void Configure(EntityTypeBuilder<TrainerPublicProfile> builder)
    {
        builder.ToTable("trainer_public_profiles");

        builder.HasKey(tpp => tpp.Id);

        builder.Property(tpp => tpp.Id)
            .ValueGeneratedNever();

        builder.Property(tpp => tpp.TrainerId)
            .IsRequired();

        builder.Property(tpp => tpp.GymId)
            .IsRequired();

        builder.Property(tpp => tpp.DisplayRating)
            .HasColumnType("decimal(3,2)")
            .IsRequired();

        builder.Property(tpp => tpp.CoachOfTheMonthCount)
            .IsRequired();

        builder.Property(tpp => tpp.UpdatedAt)
            .IsRequired();

        builder.HasMany(tpp => tpp.Testimonials)
            .WithOne()
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tpp => new { tpp.TrainerId, tpp.GymId })
            .IsUnique();
    }
}
