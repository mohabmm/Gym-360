using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.ClearKioskPin;

internal sealed class ClearKioskPinCommandHandler : ICommandHandler<ClearKioskPinCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IKioskPinHasher _hasher;

    public ClearKioskPinCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        IKioskPinHasher hasher)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
    }

    public async Task<Result> Handle(ClearKioskPinCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        if (gym.KioskPinHash is null)
            return Result.Failure(GymErrors.KioskPinNotSet);

        if (!_hasher.Verify(request.CurrentPin, gym.KioskPinHash))
            return Result.Failure(GymErrors.KioskPinCurrentIncorrect);

        gym.ClearKioskPin();
        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
