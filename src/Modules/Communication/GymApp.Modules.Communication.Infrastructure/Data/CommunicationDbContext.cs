using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Data;

public sealed class CommunicationDbContext : DbContext, ICommunicationUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<UserDeviceToken> DeviceTokens => Set<UserDeviceToken>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("communication");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunicationDbContext).Assembly);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", outboxSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasFilter("[ProcessedOnUtc] IS NULL");
        });
    }
}
