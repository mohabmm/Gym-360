using GymApp.Common.Application.Tenancy;

namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

public interface IGymProvisioningService
{
    Task<GymRegistration> ProvisionAsync(
        GymProvisioningRequest request,
        CancellationToken cancellationToken = default);

    Task DeprovisionAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task RehydrateBillingAsync(GymRegistration gym, CancellationToken cancellationToken = default);
}
