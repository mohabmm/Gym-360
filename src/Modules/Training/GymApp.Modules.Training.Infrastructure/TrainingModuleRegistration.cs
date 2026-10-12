using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Application.Contracts;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using GymApp.Modules.Training.Infrastructure.Repositories;
using GymApp.Modules.Training.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Training.Infrastructure;

public static class TrainingModuleRegistration
{
    public static IServiceCollection AddTrainingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TrainingDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution()
                .AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<ITrainingUnitOfWork>(sp => sp.GetRequiredService<TrainingDbContext>());

        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
        services.AddScoped<IWorkoutAssignmentRepository, WorkoutAssignmentRepository>();
        services.AddScoped<IRemoteWorkoutConfigRepository, RemoteWorkoutConfigRepository>();

        services.AddScoped<ITrainingQueryService, TrainingQueryService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Training.Application.Services.UserDataProvider>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Training.Application.Contracts.ITrainingQueryService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Training.Application.Contracts.ITrainingQueryService).Assembly,
            includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Fills in Arabic names for the global exercise library in the current tenant scope.
    /// Safe to run on every startup (no-op once the names are in place).
    /// </summary>
    public static Task<int> BackfillExerciseArabicNamesAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default) =>
        Seeds.ExerciseArabicNameBackfill.RunAsync(
            serviceProvider.GetRequiredService<TrainingDbContext>(), cancellationToken);
}
