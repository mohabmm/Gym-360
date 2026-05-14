using GymApp.Common.Application.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Common.Infrastructure.Tenancy.Registry;

public sealed class TenantRegistryDbContext : DbContext
{
    public const string SchemaName = "gyms_registry";

    public TenantRegistryDbContext(DbContextOptions<TenantRegistryDbContext> options) : base(options) { }

    public DbSet<GymRegistration> Gyms => Set<GymRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<GymRegistration>(entity =>
        {
            entity.ToTable("gyms");
            entity.HasKey(e => e.GymId);
            entity.Property(e => e.GymId).ValueGeneratedNever();
            entity.Property(e => e.OwnerId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SchemaPrefix).HasMaxLength(20).IsRequired();
            entity.Property(e => e.SequentialNumber).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.GymCode).HasMaxLength(6);

            entity.HasIndex(e => e.SchemaPrefix).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.SequentialNumber).IsUnique();
            entity.HasIndex(e => e.GymCode).IsUnique().HasFilter("[GymCode] IS NOT NULL");
        });
    }
}
