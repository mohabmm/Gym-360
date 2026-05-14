using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.CreateRecurringSessions;

public sealed record CreateRecurringSessionsCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    DayOfWeek DayOfWeek,
    TimeOnly Time,
    int DurationMinutes,
    string SessionType,
    int WeeksAhead) : ICommand<List<Guid>>;
