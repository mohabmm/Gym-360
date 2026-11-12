using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.RespondToSession;

/// <summary>pt.dual_session_logging: the trainee confirms a logged session, or says "not me".</summary>
public sealed record RespondToSessionCommand(
    Guid SessionId,
    Guid MemberId,
    bool Confirmed,
    string? Reason) : ICommand;
