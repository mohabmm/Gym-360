using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateBranding;

internal sealed class UpdateBrandingCommandHandler : ICommandHandler<UpdateBrandingCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public UpdateBrandingCommandHandler(
        IGymRepository gymRepository,
        IGymPlanService gymPlanService,
        IGymsUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        // Feature gate: gym_branding
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "gym_branding", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable("gym_branding", currentPlan, "Pro"));
        }

        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var branding = new Branding(
            request.LogoUrl,
            request.PrimaryColor,
            request.SecondaryColor,
            request.CoverPhotoUrl);

        gym.UpdateBranding(branding);

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Real-time fan-out: SignalR (web/mobile foreground) handler in API host
        // broadcasts to gym:{id} group so connected trainee/trainer/staff clients
        // invalidate cached branding without re-login. Mobile foreground covered
        // by SignalR; background refresh deferred (login fetch refreshes).
        await _publisher.Publish(
            new GymBrandingUpdatedIntegrationEvent(
                request.GymId,
                request.LogoUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.CoverPhotoUrl),
            cancellationToken);

        return Result.Success();
    }
}
