using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.BookSession;

public sealed record BookSessionCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    DateOnly Date,
    TimeOnly Time,
    int DurationMinutes,
    string SessionType,
    string? Focus,
    Guid? SessionPackageId = null) : ICommand<Guid>;
