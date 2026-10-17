using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.RegisterGym;

internal sealed class RegisterGymCommandHandler : ICommandHandler<RegisterGymCommand, Guid>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly TenantGymProvisioningWriter _tenantWriter;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public RegisterGymCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        TenantGymProvisioningWriter tenantWriter,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _tenantWriter = tenantWriter;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result<Guid>> Handle(RegisterGymCommand request, CancellationToken cancellationToken)
    {
        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.ZipCode,
            request.Address.Latitude,
            request.Address.Longitude);

        var contact = new ContactInfo(
            request.Contact.Email,
            request.Contact.Phone,
            request.Contact.Website);

        var gymCode = GymCode.Generate();

        var gym = Gym.Create(
            request.OwnerId,
            new GymDetails
            {
                Name = request.Name,
                Description = request.Description,
                Address = address,
                ContactInfo = contact,
                GymCode = gymCode
            });

        // Endpoint is platform_admin-only; admin-created gyms are pre-approved so
        // staff/member flows work immediately without a separate approve call.
        // Pass AutoApprove=false to leave the gym Pending — used by self-serve owner
        // signup flows where admin reviews + approves/rejects.
        if (request.AutoApprove)
            gym.Approve();

        // Soft mode: single shared schema, write directly via the request-scoped repository.
        if (!_tenantContext.IsHardIsolation)
        {
            await _gymRepository.AddAsync(gym, cancellationToken);
            if (request.AutoApprove)
                _integrationEvents.Enqueue(new GymApprovedIntegrationEvent(gym.Id, request.OwnerId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(gym.Id);
        }

        // Hard mode: provision a fresh per-gym tenant first, then persist inside it.
        await _tenantWriter.ProvisionAndSaveAsync(gym, request.AutoApprove, cancellationToken);

        return Result.Success(gym.Id);
    }
}
