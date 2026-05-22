using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gamification.Shared;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Gamification.Application.Abstractions;
using GymApp.Modules.Gamification.Application.Contracts;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Data;
using GymApp.Modules.Gamification.Infrastructure.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Gamification.Infrastructure;

public static class GamificationModuleRegistration
{
    public static IServiceCollection AddGamificationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<GamificationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString)
                .UseTenantSchemaResolution();
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<IGamificationUnitOfWork>(sp => sp.GetRequiredService<GamificationDbContext>());

        // Repositories
        services.AddScoped<IMemberLeaderboardRepository, MemberLeaderboardRepository>();
        services.AddScoped<ITrainerRatingRepository, TrainerRatingRepository>();
        services.AddScoped<ITrainerMonthlyScoreRepository, TrainerMonthlyScoreRepository>();
        services.AddScoped<ITrainerPublicProfileRepository, TrainerPublicProfileRepository>();
        services.AddScoped<ILeaderboardOptOutRepository, LeaderboardOptOutRepository>();

        // Services
        services.AddScoped<IGamificationQueryService, GamificationQueryService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Gamification.Application.Services.UserDataProvider>();
        services.AddScoped<ITrainerStatsLookup, TrainerStatsLookup>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Gamification.Application.Abstractions.IGamificationUnitOfWork).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Gamification.Application.Abstractions.IGamificationUnitOfWork).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
