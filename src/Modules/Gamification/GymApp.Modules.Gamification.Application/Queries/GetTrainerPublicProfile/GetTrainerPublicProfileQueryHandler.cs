using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerPublicProfile;

internal sealed class GetTrainerPublicProfileQueryHandler
    : IQueryHandler<GetTrainerPublicProfileQuery, TrainerPublicProfileResponse>
{
    private readonly ITrainerPublicProfileRepository _profileRepository;

    public GetTrainerPublicProfileQueryHandler(ITrainerPublicProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result<TrainerPublicProfileResponse>> Handle(
        GetTrainerPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<TrainerPublicProfileResponse>(GamificationErrors.ProfileNotFound);
        }

        var testimonials = profile.Testimonials
            .Where(t => t.IsApproved)
            .Select(t => new TestimonialDto(
                t.Id,
                t.CommentText,
                t.IsApproved,
                t.CreatedAt))
            .ToList();

        var response = new TrainerPublicProfileResponse(
            profile.TrainerId,
            profile.GymId,
            profile.DisplayRating,
            profile.CoachOfTheMonthCount,
            testimonials);

        return Result.Success(response);
    }
}
