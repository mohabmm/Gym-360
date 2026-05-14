using GymApp.Modules.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class MemberLogConfiguration : IEntityTypeConfiguration<MemberLog>
{
    public void Configure(EntityTypeBuilder<MemberLog> builder)
    {
        builder.ToTable("session_member_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.SessionId)
            .IsRequired();

        builder.Property(e => e.Rating)
            .IsRequired();

        builder.Property(e => e.PerceivedExertion)
            .IsRequired();

        builder.Property(e => e.PersonalNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.Discomfort)
            .HasMaxLength(1000);

        builder.Property(e => e.Satisfaction)
            .IsRequired();

        builder.Property(e => e.LoggedAt)
            .IsRequired();

        builder.HasIndex(e => e.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_session_member_logs_session");
    }
}
