using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.IntegrationEvents;

namespace GymApp.Modules.Billing.Application.Commands.TrialRecaps;

internal sealed class SendTrialRecapCommandHandler : ICommandHandler<SendTrialRecapCommand>
{
    private readonly ISessionPackageRepository _packages;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingUnitOfWork _unitOfWork;

    public SendTrialRecapCommandHandler(
        ISessionPackageRepository packages,
        IFeatureFlagService featureFlags,
        IIntegrationEventPublisher integrationEvents,
        IDateTimeProvider clock,
        IBillingUnitOfWork unitOfWork)
    {
        _packages = packages;
        _featureFlags = featureFlags;
        _integrationEvents = integrationEvents;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SendTrialRecapCommand request, CancellationToken cancellationToken)
    {
        var gate = await TrialRecapGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return gate;

        var package = await _packages.GetByIdAsync(request.PackageId, cancellationToken);
        if (package is null || package.GymId != request.GymId)
            return Result.Failure(BillingErrors.PackageNotFound);

        var recap = TrialRecap.Create(request.Note, request.RecommendedSessions, _clock.UtcNow);
        if (recap.IsFailure)
            return recap;

        var sent = package.SendRecap(request.TrainerId, recap.Value);
        if (sent.IsFailure)
            return sent;

        _packages.Update(package);
        _integrationEvents.Enqueue(new TrialRecapSentIntegrationEvent(
            package.Id, package.GymId, package.MemberId, package.TrainerId, recap.Value.RecommendedSessions));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>pt.trial_recap needs the trial feature it builds on.</summary>
internal static class TrialRecapGate
{
    public static async Task<Result> CheckAsync(IFeatureFlagService flags, Guid gymId, CancellationToken cancellationToken)
    {
        foreach (var key in new[] { FeatureKeys.PtTrialSessions, FeatureKeys.PtTrialRecap })
        {
            if (!await flags.IsEnabledAsync(gymId, key, cancellationToken))
                return Result.Failure(FeatureFlagErrors.FeatureDisabled(key));
        }

        return Result.Success();
    }
}
