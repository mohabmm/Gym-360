using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.LogMemberSession;

public sealed record LogMemberSessionCommand(
    Guid SessionId,
    Guid MemberId,
    int Rating,
    int PerceivedExertion,
    string? PersonalNotes,
    string? Discomfort,
    int Satisfaction) : ICommand;
