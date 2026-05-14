using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<UserResponse>;
