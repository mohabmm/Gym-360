using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionExerciseLogs;

public sealed record GetSessionExerciseLogsQuery(
    Guid SessionId,
    Guid CallerId) : IQuery<IReadOnlyList<SessionExerciseLogDto>>;
