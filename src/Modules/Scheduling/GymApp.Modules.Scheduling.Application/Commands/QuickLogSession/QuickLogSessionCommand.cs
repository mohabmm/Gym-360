using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

/// <summary>
/// pt.dual_session_logging: the coach logs a session that just happened with one tap, no
/// booking needed. <paramref name="Notes"/> is the optional second tap.
/// </summary>
public sealed record QuickLogSessionCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    string? Notes) : ICommand<QuickLogSessionResult>;

/// <param name="RemainingSessions">Sessions left with this coach after this one is counted.</param>
/// <param name="AlreadyLogged">True when the same coach logged this client in the last few minutes (double tap): nothing new was logged.</param>
public sealed record QuickLogSessionResult(Guid SessionId, int RemainingSessions, bool AlreadyLogged);
