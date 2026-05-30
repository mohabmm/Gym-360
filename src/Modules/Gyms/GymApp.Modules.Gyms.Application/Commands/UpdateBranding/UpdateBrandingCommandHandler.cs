using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateBranding;

internal sealed class UpdateBrandingCommandHandler : ICommandHandler<UpdateBrandingCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public UpdateBrandingCommandHandler(
        IGymRepository gymRepository,
        IGymPlanService gymPlanService,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
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

        // Real-time fan-out: SignalR (web/mobile foreground) handler in API host
        // broadcasts to gym:{id} group so connected trainee/trainer/staff clients
        // invalidate cached branding without re-login. Mobile foreground covered
        // by SignalR; background refresh deferred (login fetch refreshes).
        _integrationEvents.Enqueue(
            new GymBrandingUpdatedIntegrationEvent(
                request.GymId,
                request.LogoUrl,
                request.PrimaryColor,
                request.SecondaryColor,
                request.CoverPhotoUrl));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
