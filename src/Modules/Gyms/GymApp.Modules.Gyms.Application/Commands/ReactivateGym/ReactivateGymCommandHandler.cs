using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.ReactivateGym;

internal sealed class ReactivateGymCommandHandler : ICommandHandler<ReactivateGymCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public ReactivateGymCommandHandler(IGymRepository gymRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReactivateGymCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var result = gym.Reactivate();
        if (result.IsFailure)
            return result;

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
