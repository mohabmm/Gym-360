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
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<CommunicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString)
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
