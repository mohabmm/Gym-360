using GymApp.Common.Application.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Common.Infrastructure.Outbox;

public sealed class OutboxDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public OutboxDbContext(DbContextOptions<OutboxDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasFilter("[ProcessedOnUtc] IS NULL");
        });
    }
}
