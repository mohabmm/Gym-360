using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

/// <summary>
/// What a quick-log decision needs: gym-local now, the requested slot, sessions left with the coach,
/// and the same session if it was already recorded.
/// </summary>
internal sealed record QuickLogState(DateTime NowLocal, SessionSchedule Schedule, int? Remaining, Session? Recent);

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

    /// <param name="doubleTapWindow">
    /// Without a picked time (one-tap clients), a quick-log by the same coach for the same trainee within
    /// this window counts as already recorded. With a picked time, the same slot does.
    /// </param>
    public async Task<QuickLogState> LoadAsync(
        QuickLogSessionCommand request, TimeSpan doubleTapWindow, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var nowLocal = QuickLogSchedule.NowLocal(nowUtc);
        var schedule = QuickLogSchedule.Resolve(request, nowLocal);
        var remaining = await _packages.GetRemainingWithTrainerAsync(
            request.MemberId, request.TrainerId, request.GymId, cancellationToken);
        var recent = QuickLogSchedule.IsExplicit(request)
            ? await _sessions.FindQuickLogAtAsync(
                new SessionParticipants(request.GymId, request.TrainerId, request.MemberId), schedule, cancellationToken)
            : await _sessions.FindRecentQuickLogAsync(
                request.TrainerId, request.MemberId, nowUtc - doubleTapWindow, cancellationToken);
        return new QuickLogState(nowLocal, schedule, remaining, recent);
    }
}
