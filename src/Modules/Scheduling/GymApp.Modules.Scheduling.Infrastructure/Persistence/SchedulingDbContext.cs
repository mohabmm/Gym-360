using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext : DbContext, ISchedulingUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<TrainerAvailability> TrainerAvailabilities => Set<TrainerAvailability>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<BlockedDate> BlockedDates => Set<BlockedDate>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<TrainerLog> TrainerLogs => Set<TrainerLog>();
    public DbSet<MemberLog> MemberLogs => Set<MemberLog>();
    public DbSet<SessionExerciseLog> SessionExerciseLogs => Set<SessionExerciseLog>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Lightweight read model for cross-module reads from the gyms schema.
    /// </summary>
    internal DbSet<GymSessionPolicyReadModel> GymSessionPolicies => Set<GymSessionPolicyReadModel>();

    /// <summary>
    /// Lightweight read model for cross-module reads from the billing schema.
    /// Used to validate session package expiry at booking time.
    /// </summary>
    internal DbSet<SessionPackageReadModel> SessionPackages => Set<SessionPackageReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("scheduling");
        var gymsSchema = _tenantContext.SchemaFor("gyms");
        var billingSchema = _tenantContext.SchemaFor("billing");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);

        // Outbox table is owned by OutboxDbContext (g{N}_outbox.outbox_messages).
        // ProcessOutboxMessagesJob reads only from that schema, so writes from this
        // module must target the same table. Excluded from migrations because
        // OutboxDbContext.EnsureCreated owns the schema.
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", outboxSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasFilter("[ProcessedOnUtc] IS NULL");
        });

        // Read model mapped to the gyms schema — read-only cross-module access.
        // ExcludeFromMigrations: the owning module (Gyms) creates and manages this table.
        modelBuilder.Entity<GymSessionPolicyReadModel>(entity =>
        {
            entity.ToTable("gym_session_policies", gymsSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GymId).IsRequired();
            entity.Property(e => e.CancellationWindowHours).IsRequired();
            entity.Property(e => e.NoshowThreshold).IsRequired();
            entity.Property(e => e.NoshowWindowDays).IsRequired();
            entity.Property(e => e.NoshowAction).HasMaxLength(50).IsRequired();
        });

        // Read model mapped to the billing schema — read-only cross-module access.
        // ExcludeFromMigrations: the owning module (Billing) creates and manages this table.
        modelBuilder.Entity<SessionPackageReadModel>(entity =>
        {
            entity.ToTable("session_packages", billingSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GymId).IsRequired();
            entity.Property(e => e.MemberId).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TotalSessions).IsRequired();
            entity.Property(e => e.RemainingSessions).IsRequired();
            entity.Property(e => e.ExpiresAt);
        });
    }
}
