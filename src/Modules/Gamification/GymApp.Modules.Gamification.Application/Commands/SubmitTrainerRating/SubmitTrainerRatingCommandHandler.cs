using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Commands.SubmitTrainerRating;

internal sealed class SubmitTrainerRatingCommandHandler : ICommandHandler<SubmitTrainerRatingCommand, Guid>
{
    private readonly ITrainerRatingRepository _ratingRepository;
    private readonly ITrainerPublicProfileRepository _profileRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGamificationUnitOfWork _unitOfWork;

    public SubmitTrainerRatingCommandHandler(
        ITrainerRatingRepository ratingRepository,
        ITrainerPublicProfileRepository profileRepository,
        IGymPlanService gymPlanService,
        IGamificationUnitOfWork unitOfWork)
    {
        _ratingRepository = ratingRepository;
        _profileRepository = profileRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SubmitTrainerRatingCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "trainer_ratings", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("trainer_ratings", currentPlan, "Pro"));
        }

        var existingRating = await _ratingRepository.GetBySessionAsync(request.SessionId, cancellationToken);
        if (existingRating is not null)
        {
            return Result.Failure<Guid>(GamificationErrors.AlreadyRatedForSession);
        }

        var ratingResult = TrainerRating.Create(
            request.GymId,
            request.TrainerId,
            request.ClientId,
            request.SessionId,
            request.Stars,
            request.Comment);

        if (ratingResult.IsFailure)
        {
            return Result.Failure<Guid>(ratingResult.Error);
        }

        await _ratingRepository.AddAsync(ratingResult.Value, cancellationToken);

        // If the rating carries a written comment, queue it as a pending testimonial
        // so gym staff can moderate it before it appears on the trainer's public profile.
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            var profile = await _profileRepository.GetByTrainerAndGymAsync(
                request.TrainerId, request.GymId, cancellationToken);

            if (profile is null)
            {
                var created = TrainerPublicProfile.Create(request.TrainerId, request.GymId);
                if (created.IsSuccess)
                {
                    profile = created.Value;
                    profile.AddTestimonial(request.Comment!, approved: false);
                    await _profileRepository.AddAsync(profile, cancellationToken);
                }
            }
            else
            {
                profile.AddTestimonial(request.Comment!, approved: false);
                _profileRepository.Update(profile);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ratingResult.Value.Id);
    }
}
