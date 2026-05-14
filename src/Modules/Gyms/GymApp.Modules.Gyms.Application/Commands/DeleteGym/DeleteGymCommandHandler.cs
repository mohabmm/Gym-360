using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gyms.Application.Commands.DeleteGym;

internal sealed class DeleteGymCommandHandler : ICommandHandler<DeleteGymCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IGymProvisioner _provisioner;
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;

    public DeleteGymCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        IGymProvisioner provisioner,
        ITenantContext tenantContext,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _provisioner = provisioner;
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public async Task<Result> Handle(DeleteGymCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        // Cross-module fan-out runs synchronously via MediatR (matches RegisterGym /
        // Approve pattern). Identity handler must commit membership cleanup before
        // we drop the per-gym schemas, otherwise the gym row is gone while users
        // still carry orphan memberships.
        await _publisher.Publish(new GymDeletedIntegrationEvent(gym.Id), cancellationToken);

        if (!_tenantContext.IsHardIsolation)
        {
            _gymRepository.Remove(gym);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Hard mode: schema drop wipes the per-gym gyms.gyms row plus every other
        // g{N}_* table. Do NOT touch IGymRepository / per-gym DbContext after this
        // point — the schema and its scoped EF state are gone.
        await _provisioner.DeprovisionAsync(gym.Id, cancellationToken);
        return Result.Success();
    }
}
