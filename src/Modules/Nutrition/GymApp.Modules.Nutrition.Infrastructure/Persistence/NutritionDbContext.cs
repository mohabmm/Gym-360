using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence;

public sealed class NutritionDbContext : DbContext, INutritionUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public NutritionDbContext(DbContextOptions<NutritionDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<FoodLog> FoodLogs => Set<FoodLog>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<WaterLog> WaterLogs => Set<WaterLog>();
    public DbSet<WaterEntry> WaterEntries => Set<WaterEntry>();
    public DbSet<FoodDatabase> FoodDatabaseItems => Set<FoodDatabase>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanDay> MealPlanDays => Set<MealPlanDay>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("nutrition");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NutritionDbContext).Assembly);

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
