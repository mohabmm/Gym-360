using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetPendingTestimonials;

internal sealed class GetPendingTestimonialsQueryHandler
    : IQueryHandler<GetPendingTestimonialsQuery, List<PendingTestimonialDto>>
{
    private const string FeatureCode = "testimonial_moderation";

    private readonly ITrainerPublicProfileRepository _profileRepository;
    private readonly IGymPlanService _gymPlanService;

    public GetPendingTestimonialsQueryHandler(
        ITrainerPublicProfileRepository profileRepository,
        IGymPlanService gymPlanService)
    {
        _profileRepository = profileRepository;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<List<PendingTestimonialDto>>> Handle(
        GetPendingTestimonialsQuery request,
        CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, FeatureCode, cancellationToken);
        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(
                request.GymId, cancellationToken);
            return Result.Failure<List<PendingTestimonialDto>>(
                PlanErrors.FeatureNotAvailable(FeatureCode, currentPlan, "Pro"));
        }

        var profiles = await _profileRepository.ListWithPendingTestimonialsByGymAsync(
            request.GymId, cancellationToken);

        var dtos = profiles
            .SelectMany(profile => profile.Testimonials
                .Where(t => !t.IsApproved && t.ModeratedAt == null)
                .Select(t => new PendingTestimonialDto(
                    t.Id,
                    profile.TrainerId,
                    profile.GymId,
                    t.CommentText,
                    t.CreatedAt)))
            .OrderByDescending(d => d.CreatedAt)
            .ToList();

        return Result.Success(dtos);
    }
}
