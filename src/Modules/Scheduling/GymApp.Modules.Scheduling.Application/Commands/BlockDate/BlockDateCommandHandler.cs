using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;

namespace GymApp.Modules.Scheduling.Application.Commands.BlockDate;

internal sealed class BlockDateCommandHandler : ICommandHandler<BlockDateCommand>
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public BlockDateCommandHandler(
        ITrainerAvailabilityRepository availabilityRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _availabilityRepository = availabilityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(BlockDateCommand request, CancellationToken cancellationToken)
    {
        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var isNew = availability is null;
        if (isNew)
        {
            availability = TrainerAvailability.Create(request.TrainerId, request.GymId);
            await _availabilityRepository.AddAsync(availability, cancellationToken);
        }

        var result = availability!.BlockDate(request.Date, request.Reason, request.AutoNotifyClients);
        if (result.IsFailure)
            return result;

        if (!isNew)
            _availabilityRepository.Update(availability);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
