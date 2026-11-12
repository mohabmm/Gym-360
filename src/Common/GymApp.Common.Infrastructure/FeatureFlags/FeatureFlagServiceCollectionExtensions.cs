using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Infrastructure.FeatureFlags.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GymApp.Common.Infrastructure.FeatureFlags;

public static class FeatureFlagServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeatureFlagOptions>(configuration.GetSection(FeatureFlagOptions.SectionName));

        // Global (cross-tenant) context: never UseTenantSchemaResolution.
        services.AddDbContext<FeatureFlagsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Database"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", FeatureFlagsDbContext.SchemaName)));

        services.AddScoped<FeatureFlagCache>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IFeatureFlagAdminService, FeatureFlagAdminService>();

        // The API host replaces this with a SignalR notifier.
        services.TryAddSingleton<IFeatureFlagChangeNotifier, NullFeatureFlagChangeNotifier>();

        return services;
    }
}

internal sealed class NullFeatureFlagChangeNotifier : IFeatureFlagChangeNotifier
{
    public Task NotifyGymsChangedAsync(IReadOnlyCollection<Guid> gymIds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
