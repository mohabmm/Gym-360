using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetRemoteWorkoutStatus;

public sealed record GetRemoteWorkoutStatusQuery(
    Guid ClientId,
    Guid GymId) : IQuery<RemoteWorkoutStatusDto>;
