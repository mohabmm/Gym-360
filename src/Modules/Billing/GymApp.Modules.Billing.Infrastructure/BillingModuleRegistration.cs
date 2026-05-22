using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Billing.Infrastructure.Jobs;
using GymApp.Modules.Billing.Infrastructure.Repositories;
using GymApp.Modules.Billing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace GymApp.Modules.Billing.Infrastructure;

public static class BillingModuleRegistration
{
    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Resolve the connection string lazily inside the options lambda so it
        // reflects every configuration source layered on after registration
        // (e.g. test-host overrides). Capturing it eagerly into a local pinned
        // this module to a stale value and split it onto a different database
        // than the lazily-resolved modules. Keep consistent with the others.
        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution();
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<IBillingUnitOfWork>(sp => sp.GetRequiredService<BillingDbContext>());

        // Repositories
        services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISessionPackageTemplateRepository, SessionPackageTemplateRepository>();
        services.AddScoped<ISessionPackageRepository, SessionPackageRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IPlanFeatureRepository, PlanFeatureRepository>();
        services.AddScoped<IGymSubscriptionRepository, GymSubscriptionRepository>();
        services.AddScoped<IGymFeatureOverrideRepository, GymFeatureOverrideRepository>();
        services.AddScoped<IPlanChangeLogRepository, PlanChangeLogRepository>();
        services.AddScoped<IPlanStorageLimitRepository, PlanStorageLimitRepository>();
        services.AddScoped<IGymStorageUsageRepository, GymStorageUsageRepository>();
        services.AddScoped<ITrainerEarningsRepository, TrainerEarningsRepository>();

        // Services
        services.AddScoped<IGymPlanService, GymPlanService>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Billing.Application.Services.UserDataProvider>();
        services.AddScoped<IPlatformAnalyticsReader, PlatformAnalyticsReader>();
        services.AddScoped<ITrainerAssignmentLookup, TrainerAssignmentLookup>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(GymApp.Modules.Billing.Application.Abstractions.IBillingUnitOfWork).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Billing.Application.Abstractions.IBillingUnitOfWork).Assembly,
            includeInternalTypes: true);

        // Background job: expire session packages daily
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey(nameof(ProcessExpiredSessionPackagesJob));
            q.AddJob<ProcessExpiredSessionPackagesJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ProcessExpiredSessionPackagesJob)}-trigger")
                .WithCronSchedule("0 0 2 * * ?")); // daily at 02:00 UTC
        });

        return services;
    }
}
