using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetClientProgress;

public sealed record GetClientProgressQuery(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId) : IQuery<ClientProgressResponse>;
