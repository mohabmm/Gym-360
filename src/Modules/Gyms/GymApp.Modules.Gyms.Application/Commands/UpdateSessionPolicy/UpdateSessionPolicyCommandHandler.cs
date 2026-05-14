using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSessionPolicy;

internal sealed class UpdateSessionPolicyCommandHandler : ICommandHandler<UpdateSessionPolicyCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymSessionPolicyRepository _policyRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateSessionPolicyCommandHandler(
        IGymRepository gymRepository,
        IGymSessionPolicyRepository policyRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSessionPolicyCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        if (!Enum.TryParse<NoshowAction>(request.NoshowAction, ignoreCase: true, out var noshowAction))
            return Result.Failure(new Error("Gyms.InvalidNoshowAction", "The specified no-show action is invalid."));

        var policy = await _policyRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        if (policy is null)
        {
            policy = GymSessionPolicy.Create(request.GymId);
            policy.Update(
                request.CancellationWindowHours,
                request.NoshowThreshold,
                request.NoshowWindowDays,
                noshowAction,
                Guid.Empty); // Updated by will be set from auth context in presentation layer

            await _policyRepository.AddAsync(policy, cancellationToken);
        }
        else
        {
            policy.Update(
                request.CancellationWindowHours,
                request.NoshowThreshold,
                request.NoshowWindowDays,
                noshowAction,
                Guid.Empty);

            _policyRepository.Update(policy);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
