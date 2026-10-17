using System.Text;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Gyms.Application.Commands.RegisterGym;

/// <summary>
/// Hard-isolation registration: provisions a fresh per-gym tenant (registry row +
/// g{N}_* schemas + tables), then persists the aggregate inside a new scope pinned
/// to that prefix so EF resolves the correct g{N}_gyms.gyms table.
/// </summary>
internal sealed class TenantGymProvisioningWriter
{
    private readonly IGymProvisioner _provisioner;
    private readonly IServiceScopeFactory _scopeFactory;

    public TenantGymProvisioningWriter(IGymProvisioner provisioner, IServiceScopeFactory scopeFactory)
    {
        _provisioner = provisioner;
        _scopeFactory = scopeFactory;
    }

    public async Task ProvisionAndSaveAsync(Gym gym, bool autoApprove, CancellationToken cancellationToken)
    {
        var gymCode = gym.GymCode.Value;
        var slug = BuildSlug(gym.Name, gymCode);
        var registration = await _provisioner.ProvisionAsync(
            new GymProvisioningRequest(gym.Id, gym.OwnerId, gym.Name, slug, gymCode), cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var accessor = sp.GetRequiredService<ITenantContextAccessor>();
        accessor.Set(registration.GymId, registration.SchemaPrefix, isOwnerScope: false);

        var scopedRepo = sp.GetRequiredService<IGymRepository>();
        var scopedUow = sp.GetRequiredService<IGymsUnitOfWork>();

        await scopedRepo.AddAsync(gym, cancellationToken);

        // Cross-module fan-out: Billing handler subscribes the gym to a default plan
        // so plan-gated features (custom_exercise_library, etc.) work post-register.
        // Only fired when the gym is auto-approved; pending gyms must be approved
        // separately via /admin/gyms/{gymId}/approve which raises the same event.
        // Enqueue via the tenant-pinned scope's publisher BEFORE scopedUow.SaveChanges so the
        // outbox row is written by the scoped GymsDbContext into the new gym's g{N}_outbox
        // schema (not the admin request scope). The Hard-mode outbox processor then delivers
        // it with the gym's tenant set, so Billing's auto-subscribe writes to g{N}_billing.
        if (autoApprove)
        {
            var scopedIntegrationEvents = sp.GetRequiredService<IIntegrationEventPublisher>();
            scopedIntegrationEvents.Enqueue(new GymApprovedIntegrationEvent(gym.Id, gym.OwnerId));
        }

        await scopedUow.SaveChangesAsync(cancellationToken);
    }

    private static string BuildSlug(string name, string gymCode)
    {
        var sb = new StringBuilder();
        foreach (var ch in (name ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        if (slug.Length == 0) slug = "gym";
        if (slug.Length > 80) slug = slug.Substring(0, 80);
        return $"{slug}-{gymCode.ToLowerInvariant()}";
    }
}
