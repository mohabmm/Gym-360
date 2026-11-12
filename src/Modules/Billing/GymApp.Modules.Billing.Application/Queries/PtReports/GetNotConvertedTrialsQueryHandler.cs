using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Services;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.PtReports;

internal sealed class GetNotConvertedTrialsQueryHandler
    : IQueryHandler<GetNotConvertedTrialsQuery, IReadOnlyList<NotConvertedTrialDto>>
{
    private readonly ISessionPackageRepository _packages;
    private readonly IUserDisplayLookup _users;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IDateTimeProvider _clock;

    public GetNotConvertedTrialsQueryHandler(
        ISessionPackageRepository packages,
        IUserDisplayLookup users,
        IFeatureFlagService featureFlags,
        IDateTimeProvider clock)
    {
        _packages = packages;
        _users = users;
        _featureFlags = featureFlags;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<NotConvertedTrialDto>>> Handle(
        GetNotConvertedTrialsQuery request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtTrialEndAlerts, cancellationToken))
            return Result.Failure<IReadOnlyList<NotConvertedTrialDto>>(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtTrialEndAlerts));

        var packages = await _packages.GetByGymWithUsageAsync(request.GymId, cancellationToken);
        var trials = PtMetricsCalculator.NotConverted(PtFacts.Packages(packages), _clock.UtcNow);

        var names = await _users.GetDisplayNamesAsync(
            trials.SelectMany(t => new[] { t.MemberId, t.TrainerId }).Distinct(), cancellationToken);

        IReadOnlyList<NotConvertedTrialDto> result = trials
            .Select(t => new NotConvertedTrialDto(
                t.PackageId,
                t.MemberId,
                NameOf(names, t.MemberId),
                t.TrainerId,
                NameOf(names, t.TrainerId),
                t.TrialStartedAtUtc,
                t.FinishedAtUtc,
                t.DaysSinceFinished,
                t.ConversionWindowClosed))
            .ToList();

        return Result.Success(result);
    }

    private static string NameOf(IReadOnlyDictionary<Guid, UserDisplay> names, Guid id) =>
        names.TryGetValue(id, out var d) ? d.FullName : string.Empty;
}
