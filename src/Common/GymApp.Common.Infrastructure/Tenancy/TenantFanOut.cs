using GymApp.Common.Application.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GymApp.Common.Infrastructure.Tenancy;

internal sealed class TenantFanOut : ITenantFanOut
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGymRegistry _registry;
    private readonly TenancyOptions _options;

    public TenantFanOut(IServiceScopeFactory scopeFactory, IGymRegistry registry, IOptions<TenancyOptions> options)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _options = options.Value;
    }

    public async Task ForEachTenantAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        if (_options.Mode != TenancyMode.Hard)
        {
            await RunAsync(null, work, cancellationToken);
            return;
        }

        foreach (var gym in await _registry.GetActiveAsync(cancellationToken))
            await RunAsync(gym, work, cancellationToken);
    }

    private async Task RunAsync(
        GymRegistration? gym,
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        if (gym is not null)
        {
            scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>()
                .Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);
        }

        await work(scope.ServiceProvider, cancellationToken);
    }
}
