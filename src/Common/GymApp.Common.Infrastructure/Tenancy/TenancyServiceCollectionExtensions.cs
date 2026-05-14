using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Aggregator;
using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Common.Infrastructure.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
        services.AddScoped<TenantContextAccessor>();
        services.AddScoped<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextAccessor>());

        // Tenant registry (cross-tenant): always uses gyms_registry schema, never per-gym.
        services.AddDbContext<TenantRegistryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Database")));
        services.AddScoped<IGymRegistry, GymRegistryService>();

        // Provisioning (used by onboarding endpoint + bootstrap).
        services.AddSingleton<SchemaCreator>();
        services.AddScoped<GymProvisioningService>();
        services.AddScoped<IGymProvisioningService>(sp => sp.GetRequiredService<GymProvisioningService>());
        services.AddScoped<IGymProvisioner>(sp => sp.GetRequiredService<GymProvisioningService>());

        // Owner unified dashboard aggregator (cross-gym rollup).
        services.AddMemoryCache();
        services.AddScoped<IOwnerDashboardService, OwnerDashboardService>();

        return services;
    }

    public static DbContextOptionsBuilder UseTenantSchemaResolution(
        this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        return optionsBuilder;
    }
}
