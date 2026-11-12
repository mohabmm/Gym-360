using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Services;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.PtReports;

internal sealed class GetPtDashboardQueryHandler : IQueryHandler<GetPtDashboardQuery, PtDashboardDto>
{
    private readonly ISessionPackageRepository _packages;
    private readonly IUserDisplayLookup _users;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IDateTimeProvider _clock;

    public GetPtDashboardQueryHandler(
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

    public async Task<Result<PtDashboardDto>> Handle(GetPtDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtOwnerDashboard, cancellationToken))
            return Result.Failure<PtDashboardDto>(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtOwnerDashboard));

        var packages = await _packages.GetByGymWithUsageAsync(request.GymId, cancellationToken);
        var facts = PtFacts.Packages(packages);
        var coaches = PtMetricsCalculator.ByCoach(facts, PtFacts.Usages(packages), request.FromUtc, request.ToUtc);
        var names = await _users.GetDisplayNamesAsync(coaches.Select(c => c.TrainerId), cancellationToken);

        var finished = coaches.Sum(c => c.TrialsFinished);
        var converted = coaches.Sum(c => c.TrialsConverted);
        var totals = new PtDashboardTotals(
            finished,
            converted,
            finished == 0 ? 0m : Math.Round(converted * 100m / finished, 1),
            coaches.Sum(c => c.Renewals),
            coaches.Sum(c => c.NewPaidPackages),
            coaches.Sum(c => c.SessionsLogged),
            packages.Count(p => p.IsTrial && p.Status == PackageStatus.Active),
            PtMetricsCalculator.NotConverted(facts, _clock.UtcNow).Count);

        var rows = coaches
            .Select(c => new PtCoachRowDto(
                c.TrainerId,
                names.TryGetValue(c.TrainerId, out var d) ? d.FullName : string.Empty,
                c.TrialsFinished,
                c.TrialsConverted,
                c.ConversionRate,
                c.Renewals,
                c.NewPaidPackages,
                c.SessionsLogged))
            .ToList();

        return Result.Success(new PtDashboardDto(request.FromUtc, request.ToUtc, totals, rows));
    }
}
