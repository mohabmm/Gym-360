using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Commands.ToggleLeaderboardOptOut;

internal sealed class ToggleLeaderboardOptOutCommandHandler : ICommandHandler<ToggleLeaderboardOptOutCommand>
{
    private readonly ILeaderboardOptOutRepository _optOutRepository;
    private readonly IGamificationUnitOfWork _unitOfWork;

    public ToggleLeaderboardOptOutCommandHandler(
        ILeaderboardOptOutRepository optOutRepository,
        IGamificationUnitOfWork unitOfWork)
    {
        _optOutRepository = optOutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ToggleLeaderboardOptOutCommand request, CancellationToken cancellationToken)
    {
        var optOut = await _optOutRepository.GetByUserAndGymAsync(
            request.UserId, request.GymId, cancellationToken);

        if (optOut is null)
        {
            // First toggle: create as opted out (ShowOnLeaderboard = false)
            var createResult = LeaderboardOptOut.Create(request.UserId, request.GymId, false);
            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }

            await _optOutRepository.AddAsync(createResult.Value, cancellationToken);
        }
        else
        {
            optOut.Toggle();
            _optOutRepository.Update(optOut);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
