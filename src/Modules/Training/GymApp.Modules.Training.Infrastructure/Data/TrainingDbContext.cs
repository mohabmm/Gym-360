using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Data;

public sealed class TrainingDbContext : DbContext, ITrainingUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public TrainingDbContext(DbContextOptions<TrainingDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<ProgramWeek> ProgramWeeks => Set<ProgramWeek>();
    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
    public DbSet<LogSet> LogSets => Set<LogSet>();
    public DbSet<WorkoutAssignment> WorkoutAssignments => Set<WorkoutAssignment>();
    public DbSet<RemoteWorkoutConfig> RemoteWorkoutConfigs => Set<RemoteWorkoutConfig>();
    public DbSet<PlanTemplate> PlanTemplates => Set<PlanTemplate>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _tenantContext.SchemaFor("training");
        var outboxSchema = _tenantContext.SchemaFor("outbox");
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrainingDbContext).Assembly);

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
