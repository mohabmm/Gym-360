using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.UserId).IsRequired();
        builder.HasIndex(o => o.UserId).IsUnique();

        builder.Property(o => o.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.BillingEmail).HasMaxLength(256).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasOne(o => o.Contract)
            .WithOne()
            .HasForeignKey<Contract>(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
