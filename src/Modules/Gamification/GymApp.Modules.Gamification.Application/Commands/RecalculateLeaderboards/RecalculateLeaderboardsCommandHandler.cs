using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Domain.ValueObjects;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Commands.RecalculateLeaderboards;

internal sealed class RecalculateLeaderboardsCommandHandler : ICommandHandler<RecalculateLeaderboardsCommand>
{
    private readonly IMemberLeaderboardRepository _leaderboardRepository;
    private readonly ILeaderboardOptOutRepository _optOutRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGamificationUnitOfWork _unitOfWork;

    public RecalculateLeaderboardsCommandHandler(
        IMemberLeaderboardRepository leaderboardRepository,
        ILeaderboardOptOutRepository optOutRepository,
        IGymPlanService gymPlanService,
        IGamificationUnitOfWork unitOfWork)
    {
        _leaderboardRepository = leaderboardRepository;
        _optOutRepository = optOutRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecalculateLeaderboardsCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "member_leaderboards", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable("member_leaderboards", currentPlan, "Pro"));
        }

        var now = DateTime.UtcNow;
        var period = LeaderboardPeriod.Create(now.Month, now.Year);

        foreach (var type in Enum.GetValues<LeaderboardType>())
        {
            var leaderboard = await _leaderboardRepository.GetByGymAndTypeAsync(
                request.GymId, type, period, cancellationToken);

            if (leaderboard is null)
            {
                var createResult = MemberLeaderboard.Create(request.GymId, period, type);
                if (createResult.IsFailure)
                    continue;

                leaderboard = createResult.Value;
                await _leaderboardRepository.AddAsync(leaderboard, cancellationToken);
            }
            else
            {
                leaderboard.ClearEntries();
            }

            leaderboard.MarkRecalculated();
            _leaderboardRepository.Update(leaderboard);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
