using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Gyms.Application.Commands.RegisterGym;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Identity.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Gyms.Application.IntegrationEvents;

// When an admin creates a standalone GymOwner (Identity), auto-provision that
// owner's first gym so they never land on the "No owned gyms found" dead-end.
// The gym's registry OwnerId is stored as the owner's USER id, which is exactly
// what GetCurrentUser resolves via GetByOwnerAsync(user.Id) to build ownedGyms.
//
// Idempotent: skips if the owner already owns a gym (also guarded by the outbox
// idempotent ledger, so a redelivery cannot double-create).
internal sealed class RegisterGymForNewOwnerHandler
    : INotificationHandler<GymOwnerCreatedIntegrationEvent>
{
    // Address/Contact are required by RegisterGym's validator but unknown at
    // owner-creation time; seed sensible placeholders the owner can edit later
    // (mirrors the admin Add-Gym form defaults).
    private const string PlaceholderPhone = "+200000000000";

    private readonly ISender _sender;
    private readonly IGymRegistry _gymRegistry;
    private readonly ILogger<RegisterGymForNewOwnerHandler> _logger;

    public RegisterGymForNewOwnerHandler(
        ISender sender,
        IGymRegistry gymRegistry,
        ILogger<RegisterGymForNewOwnerHandler> logger)
    {
        _sender = sender;
        _gymRegistry = gymRegistry;
        _logger = logger;
    }

    public async Task Handle(GymOwnerCreatedIntegrationEvent n, CancellationToken ct)
    {
        var existing = await _gymRegistry.GetByOwnerAsync(n.OwnerUserId, ct);
        if (existing.Count > 0)
            return; // owner already has a gym — nothing to provision

        var name = string.IsNullOrWhiteSpace(n.CompanyName) ? n.BillingEmail : n.CompanyName.Trim();
        var phone = string.IsNullOrWhiteSpace(n.Phone) ? PlaceholderPhone : n.Phone.Trim();

        var command = new RegisterGymCommand(
            n.OwnerUserId,
            name,
            Description: null,
            Address: new AddressDto("TBD", "Cairo", "Cairo", "EG", "00000"),
            Contact: new ContactInfoDto(n.BillingEmail, phone),
            AutoApprove: true);

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
            _logger.LogWarning(
                "RegisterGymForNewOwner: failed to auto-provision gym for owner {OwnerUserId}: {Error}",
                n.OwnerUserId, result.Error);
        else
            _logger.LogInformation(
                "RegisterGymForNewOwner: auto-provisioned gym {GymId} for new owner {OwnerUserId}",
                result.Value, n.OwnerUserId);
    }
}
