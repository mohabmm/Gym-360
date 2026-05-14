using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Commands.UnblockDate;

internal sealed class UnblockDateCommandHandler : ICommandHandler<UnblockDateCommand>
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public UnblockDateCommandHandler(
        ITrainerAvailabilityRepository availabilityRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _availabilityRepository = availabilityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnblockDateCommand request, CancellationToken cancellationToken)
    {
        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (availability is null)
            return Result.Failure(SchedulingErrors.AvailabilityNotFoundForTrainer(request.TrainerId));

        var result = availability.UnblockDate(request.Date);
        if (result.IsFailure)
            return result;

        _availabilityRepository.Update(availability);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
