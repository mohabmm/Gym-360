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
    public DbSet<OutboxMessageConsumer> OutboxMessageConsumers => Set<OutboxMessageConsumer>();

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
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasFilter("[ProcessedOnUtc] IS NULL");
        });

        modelBuilder.Entity<OutboxMessageConsumer>(entity =>
        {
            entity.ToTable("outbox_message_consumers", schema);
            // Name is part of the clustered PK; keep it well under the 900-byte key limit.
            // Namespace-qualified handler names run ~100 chars; OutboxConsumerName caps at 200.
            entity.HasKey(e => new { e.OutboxMessageId, e.Name });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });
    }
}
