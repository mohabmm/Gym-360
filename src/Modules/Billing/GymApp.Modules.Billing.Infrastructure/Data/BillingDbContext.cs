using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Data;

public sealed class BillingDbContext : DbContext, IBillingUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public BillingDbContext(DbContextOptions<BillingDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SessionPackageTemplate> SessionPackageTemplates => Set<SessionPackageTemplate>();
    public DbSet<SessionPackage> SessionPackages => Set<SessionPackage>();
    public DbSet<PackageUsage> PackageUsages => Set<PackageUsage>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<GymSubscription> GymSubscriptions => Set<GymSubscription>();
    public DbSet<GymFeatureOverride> GymFeatureOverrides => Set<GymFeatureOverride>();
    public DbSet<PlanChangeLog> PlanChangeLogs => Set<PlanChangeLog>();
    public DbSet<PlanStorageLimit> PlanStorageLimits => Set<PlanStorageLimit>();
    public DbSet<GymStorageUsage> GymStorageUsages => Set<GymStorageUsage>();
    public DbSet<TrainerEarningsRecord> TrainerEarningsRecords => Set<TrainerEarningsRecord>();

    /// <summary>
    /// Lightweight read model for cross-module reads from the gyms schema.
    /// Used by TrainerEarningsRepository to look up the configured trainer rate.
    /// </summary>
    internal DbSet<TrainerRateReadModel> TrainerRates => Set<TrainerRateReadModel>();

    /// <summary>
    /// Lightweight read model for admin analytics: counts of gyms by status and city.
    /// Reads directly from gyms.gyms; writes happen via the Gyms module.
    /// </summary>
    internal DbSet<GymSummaryReadModel> GymSummaries => Set<GymSummaryReadModel>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("billing");
        var gymsSchema = _tenantContext.SchemaFor("gyms");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

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
        modelBuilder.Entity<TrainerRateReadModel>(entity =>
        {
            entity.ToTable("trainer_rates", gymsSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GymId).IsRequired();
            entity.Property(e => e.TrainerId).IsRequired();
            entity.Property(e => e.RatePerSession)
                .HasColumnType("decimal(10,2)")
                .IsRequired();
        });

        // Read model for admin analytics. GymStatus is stored as a string on the Gyms side
        // (see GymConfiguration), so we map it as string and convert in queries.
        // ExcludeFromMigrations: the owning module (Gyms) creates and manages this table.
        modelBuilder.Entity<GymSummaryReadModel>(entity =>
        {
            entity.ToTable("gyms", gymsSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.StatusText).HasColumnName("Status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.City).HasColumnName("Address_City").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // The Gyms module stores _memberIds/_trainerIds as JSON-encoded primitive collections.
            // We only need counts, so ignore the raw columns in queries below.
            entity.Ignore(e => e.MemberCount);
            entity.Ignore(e => e.TrainerCount);
        });
    }
}
