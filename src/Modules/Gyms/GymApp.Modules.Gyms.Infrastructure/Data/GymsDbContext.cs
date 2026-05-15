using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Data;

public sealed class GymsDbContext : DbContext, IGymsUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public GymsDbContext(DbContextOptions<GymsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<GymClass> GymClasses => Set<GymClass>();
    public DbSet<ClassBooking> ClassBookings => Set<ClassBooking>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<StaffRole> StaffRoles => Set<StaffRole>();
    public DbSet<StaffRolePermission> StaffRolePermissions => Set<StaffRolePermission>();
    public DbSet<GymSessionPolicy> GymSessionPolicies => Set<GymSessionPolicy>();
    public DbSet<GymSection> GymSections => Set<GymSection>();
    public DbSet<GymSeasonalHours> GymSeasonalHours => Set<GymSeasonalHours>();
    public DbSet<TrainerRate> TrainerRates => Set<TrainerRate>();
    public DbSet<MemberNote> MemberNotes => Set<MemberNote>();
    public DbSet<MemberOnboarding> MemberOnboardings => Set<MemberOnboarding>();
    public DbSet<DataImportJob> DataImportJobs => Set<DataImportJob>();
    public DbSet<GymMember> GymMembers => Set<GymMember>();
    public DbSet<GymTrainerRequest> GymTrainerRequests => Set<GymTrainerRequest>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Lightweight read model for cross-module reads from the billing schema.
    /// Used by ProcessGymSubscriptionStatusJob to check payment due dates
    /// without referencing Billing.Infrastructure or Billing.Domain directly.
    /// </summary>
    internal DbSet<GymSubscriptionReadModel> GymSubscriptions => Set<GymSubscriptionReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("gyms");
        var billingSchema = _tenantContext.SchemaFor("billing");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymsDbContext).Assembly);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", outboxSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Content).HasColumnType("nvarchar(max)");
        });

        // Read model mapped to the billing schema — read-only cross-module access.
        // ExcludeFromMigrations: the owning module (Billing) creates and manages this table.
        modelBuilder.Entity<GymSubscriptionReadModel>(entity =>
        {
            entity.ToTable("gym_subscriptions", billingSchema, t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GymId).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NextPaymentDue);
            entity.Property(e => e.OverdueSince);
            entity.Property(e => e.GracePeriodEndsAt);
        });
    }
}
