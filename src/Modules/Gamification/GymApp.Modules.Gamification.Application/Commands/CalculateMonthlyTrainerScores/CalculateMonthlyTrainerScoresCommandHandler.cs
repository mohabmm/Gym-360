using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Commands.CalculateMonthlyTrainerScores;

internal sealed class CalculateMonthlyTrainerScoresCommandHandler : ICommandHandler<CalculateMonthlyTrainerScoresCommand>
{
    private readonly ITrainerMonthlyScoreRepository _scoreRepository;
    private readonly ITrainerPublicProfileRepository _profileRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGamificationUnitOfWork _unitOfWork;

    public CalculateMonthlyTrainerScoresCommandHandler(
        ITrainerMonthlyScoreRepository scoreRepository,
        ITrainerPublicProfileRepository profileRepository,
        IGymPlanService gymPlanService,
        IGamificationUnitOfWork unitOfWork)
    {
        _scoreRepository = scoreRepository;
        _profileRepository = profileRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CalculateMonthlyTrainerScoresCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "coach_of_the_month", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable("coach_of_the_month", currentPlan, "Business"));
        }

        var scores = await _scoreRepository.GetByGymAsync(
            request.GymId, request.Month, request.Year, cancellationToken);

        if (scores.Count == 0)
        {
            return Result.Success();
        }

        // Find highest normalized score and select as coach of the month
        var topScore = scores
            .OrderByDescending(s => s.NormalizedScore)
            .First();

        var selectResult = topScore.SelectAsCoachOfTheMonth();
        if (selectResult.IsSuccess)
        {
            _scoreRepository.Update(topScore);

            // Update public profile coach-of-the-month count
            var profile = await _profileRepository.GetByTrainerAndGymAsync(
                topScore.TrainerId, request.GymId, cancellationToken);

            if (profile is not null)
            {
                profile.IncrementCoachOfTheMonthCount();
                _profileRepository.Update(profile);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
