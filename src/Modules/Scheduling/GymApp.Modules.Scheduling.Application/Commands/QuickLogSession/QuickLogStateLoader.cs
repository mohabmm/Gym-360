using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

/// <summary>What a quick-log decision needs: the time, sessions left with the coach, and a just-logged session.</summary>
internal sealed record QuickLogState(DateTime NowUtc, int? Remaining, Session? Recent);

/// <summary>Reads <see cref="QuickLogState"/> for a coach and trainee.</summary>
internal sealed class QuickLogStateLoader
{
    private readonly ISessionRepository _sessions;
    private readonly ISessionPackageService _packages;
    private readonly IDateTimeProvider _clock;

    public QuickLogStateLoader(ISessionRepository sessions, ISessionPackageService packages, IDateTimeProvider clock)
    {
        _sessions = sessions;
        _packages = packages;
        _clock = clock;
    }

    /// <param name="doubleTapWindow">A quick-log by the same coach for the same trainee within this window counts as "recent".</param>
    public async Task<QuickLogState> LoadAsync(
        QuickLogSessionCommand request, TimeSpan doubleTapWindow, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var remaining = await _packages.GetRemainingWithTrainerAsync(
            request.MemberId, request.TrainerId, request.GymId, cancellationToken);
        var recent = await _sessions.FindRecentQuickLogAsync(
            request.TrainerId, request.MemberId, nowUtc - doubleTapWindow, cancellationToken);
        return new QuickLogState(nowUtc, remaining, recent);
    }
}
