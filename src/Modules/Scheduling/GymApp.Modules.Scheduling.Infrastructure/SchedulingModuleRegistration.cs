using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using GymApp.Modules.Scheduling.Infrastructure.Repositories;
using GymApp.Modules.Scheduling.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Scheduling.Infrastructure;

public static class SchedulingModuleRegistration
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SchedulingDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution()
                .AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<ISchedulingUnitOfWork>(sp => sp.GetRequiredService<SchedulingDbContext>());

        services.AddScoped<ITrainerAvailabilityRepository, TrainerAvailabilityRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        services.AddScoped<ISchedulingQueryService, SchedulingQueryService>();
        services.AddScoped<IGymSessionPolicyService, GymSessionPolicyService>();
        services.AddScoped<ISessionPackageService, SessionPackageService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Scheduling.Application.Services.UserDataProvider>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Scheduling.Application.Contracts.ISchedulingQueryService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Scheduling.Application.Contracts.ISchedulingQueryService).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
