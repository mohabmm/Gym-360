using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Modules.Gamification.Application.Commands.ModerateTestimonial;

internal sealed class ModerateTestimonialCommandHandler : ICommandHandler<ModerateTestimonialCommand>
{
    private readonly ITrainerPublicProfileRepository _profileRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGamificationUnitOfWork _unitOfWork;

    public ModerateTestimonialCommandHandler(
        ITrainerPublicProfileRepository profileRepository,
        IGymPlanService gymPlanService,
        IGamificationUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ModerateTestimonialCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "testimonial_moderation", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable("testimonial_moderation", currentPlan, "Pro"));
        }

        var profile = await _profileRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(GamificationErrors.ProfileNotFound);
        }

        var testimonial = profile.Testimonials.FirstOrDefault(t => t.Id == request.TestimonialId);
        if (testimonial is null)
        {
            return Result.Failure(GamificationErrors.TestimonialNotFound);
        }

        if (request.IsApproved)
        {
            testimonial.Approve(request.ModeratedBy);
        }
        else
        {
            testimonial.Reject(request.ModeratedBy);
        }

        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
