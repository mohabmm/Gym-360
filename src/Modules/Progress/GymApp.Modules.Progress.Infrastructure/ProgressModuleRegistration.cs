using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Application.Contracts;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using GymApp.Modules.Progress.Infrastructure.Repositories;
using GymApp.Modules.Progress.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Progress.Infrastructure;

public static class ProgressModuleRegistration
{
    public static IServiceCollection AddProgressModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProgressDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution()
                .AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<IProgressUnitOfWork>(sp => sp.GetRequiredService<ProgressDbContext>());

        services.AddScoped<IBodyMetricEntryRepository, BodyMetricEntryRepository>();
        services.AddScoped<IProgressPhotoRepository, ProgressPhotoRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();

        services.AddScoped<IProgressQueryService, ProgressQueryService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Progress.Application.Services.UserDataProvider>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Progress.Application.Contracts.IProgressQueryService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Progress.Application.Contracts.IProgressQueryService).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
