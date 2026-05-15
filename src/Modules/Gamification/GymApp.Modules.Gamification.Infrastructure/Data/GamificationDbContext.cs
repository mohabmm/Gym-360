using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Gamification.Application.Abstractions;
using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Data;

public sealed class GamificationDbContext : DbContext, IGamificationUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public GamificationDbContext(DbContextOptions<GamificationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<MemberLeaderboard> MemberLeaderboards => Set<MemberLeaderboard>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<TrainerRating> TrainerRatings => Set<TrainerRating>();
    public DbSet<TrainerMonthlyScore> TrainerMonthlyScores => Set<TrainerMonthlyScore>();
    public DbSet<TrainerPublicProfile> TrainerPublicProfiles => Set<TrainerPublicProfile>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<LeaderboardOptOut> LeaderboardOptOuts => Set<LeaderboardOptOut>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("gamification");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamificationDbContext).Assembly);

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
