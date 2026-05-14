using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;

namespace GymApp.Modules.Progress.Application.Commands.UpdateStreak;

internal sealed class UpdateStreakCommandHandler : ICommandHandler<UpdateStreakCommand>
{
    private readonly IStreakRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;

    public UpdateStreakCommandHandler(
        IStreakRepository repository,
        IProgressUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateStreakCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<StreakType>(request.StreakType, true, out var streakType))
        {
            return Result.Failure(new Error("Progress.InvalidStreakType", "Invalid streak type."));
        }

        var streak = await _repository.GetByUserAndTypeAsync(
            request.UserId, request.GymId, streakType, cancellationToken);

        if (streak is null)
        {
            var createResult = Streak.Create(request.UserId, request.GymId, streakType);
            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }

            streak = createResult.Value;
            streak.RecordActivity(request.ActivityDate);
            await _repository.AddAsync(streak, cancellationToken);
        }
        else
        {
            streak.RecordActivity(request.ActivityDate);
            _repository.Update(streak);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
