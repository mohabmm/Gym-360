using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Infrastructure.Data;
using GymApp.Modules.Communication.Infrastructure.Repositories;
using GymApp.Modules.Communication.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Communication.Infrastructure;

public static class CommunicationModuleRegistration
{
    public static IServiceCollection AddCommunicationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Resolve the connection string lazily inside the options lambda so it
        // reflects every configuration source layered on after registration
        // (e.g. test-host overrides). Capturing it eagerly into a local pinned
        // this module to a stale value and split it onto a different database
        // than the lazily-resolved modules. Keep consistent with the others.
        services.AddDbContext<CommunicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution();
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<ICommunicationUnitOfWork>(sp => sp.GetRequiredService<CommunicationDbContext>());

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // Services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFcmTokenStore, FcmTokenStore>();
        services.AddHttpClient<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Communication.Application.Services.UserDataProvider>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Communication.Application.Abstractions.ICommunicationUnitOfWork).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Communication.Application.Abstractions.ICommunicationUnitOfWork).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
