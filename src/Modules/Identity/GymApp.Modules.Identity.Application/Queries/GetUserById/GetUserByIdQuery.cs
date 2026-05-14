using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
