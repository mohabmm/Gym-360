using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Synchronous (not the outbox): Identity is global and its outbox rows would land in a
/// schema the Hard-mode per-gym outbox processor never drains.
/// </summary>
internal sealed class UserDataPurger : IUserDataPurger
{
    private readonly ITenantFanOut _tenantFanOut;
    private readonly ILogger<UserDataPurger> _logger;

    public UserDataPurger(ITenantFanOut tenantFanOut, ILogger<UserDataPurger> logger)
    {
        _tenantFanOut = tenantFanOut;
        _logger = logger;
    }

    public Task PurgeAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _tenantFanOut.ForEachTenantAsync(async (scope, ct) =>
        {
            foreach (var eraser in scope.GetServices<IUserDataEraser>())
            {
                var rows = await eraser.EraseAsync(userId, ct);
                _logger.LogInformation(
                    "Admin hard delete: erased {Rows} {Module} rows for user {UserId}", rows, eraser.ModuleName, userId);
            }
        }, cancellationToken);
}
