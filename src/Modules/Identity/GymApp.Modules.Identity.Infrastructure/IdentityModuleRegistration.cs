using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.Services;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using GymApp.Modules.Identity.Infrastructure.Persistence.Repositories;
using GymApp.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Identity.Infrastructure;

public static class IdentityModuleRegistration
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"));

            options.AddInterceptors(
                sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        // Register IIdentityUnitOfWork scoped to IdentityDbContext
        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();

        // Register services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<Application.Contracts.IJwtTokenService, IdentityJwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOtpSender, OtpSender>();
        services.AddScoped<IDataExportService, DataExportService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Identity.Application.Services.UserDataProvider>();
        services.AddScoped<IGymMemberLookup, GymMemberLookup>();
        services.AddScoped<IUserDisplayLookup, UserDisplayLookup>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<IUserProfileUpdater, UserProfileUpdater>();

        // Register MediatR handlers from Application assembly.
        // Pipeline behaviors are added per-module because MediatR's open behaviors only
        // apply to handlers registered in the same AddMediatR call.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.Contracts.IJwtTokenService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation validators from Application assembly
        services.AddValidatorsFromAssembly(
            typeof(Application.Contracts.IJwtTokenService).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
