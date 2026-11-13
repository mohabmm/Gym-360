using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Application.Commands.TrialRecaps;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.TrialRecaps;

internal sealed class GetMyTrialRecapQueryHandler : IQueryHandler<GetMyTrialRecapQuery, TrialRecapDto>
{
    private readonly ISessionPackageRepository _packages;
    private readonly IUserDisplayLookup _users;
    private readonly IFeatureFlagService _featureFlags;

    public GetMyTrialRecapQueryHandler(
        ISessionPackageRepository packages,
        IUserDisplayLookup users,
        IFeatureFlagService featureFlags)
    {
        _packages = packages;
        _users = users;
        _featureFlags = featureFlags;
    }

    public async Task<Result<TrialRecapDto>> Handle(GetMyTrialRecapQuery request, CancellationToken cancellationToken)
    {
        var gate = await TrialRecapGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return Result.Failure<TrialRecapDto>(gate.Error);

        var packages = await _packages.GetByMemberAsync(request.GymId, request.MemberId, cancellationToken);
        var trial = packages
            .Where(p => p.IsTrial && p.RecapSentAt is not null)
            .MaxBy(p => p.RecapSentAt);
        if (trial is null)
            return Result.Failure<TrialRecapDto>(BillingErrors.TrialRecapNotFound);

        var names = await _users.GetDisplayNamesAsync(new[] { trial.TrainerId }, cancellationToken);
        names.TryGetValue(trial.TrainerId, out var coach);

        return Result.Success(new TrialRecapDto(
            trial.Id,
            trial.TrainerId,
            coach?.FullName ?? string.Empty,
            coach?.PhotoUrl,
            trial.RecapNote!,
            trial.RecommendedSessions!.Value,
            trial.RecapSentAt!.Value,
            trial.Status == PackageStatus.Exhausted));
    }
}
