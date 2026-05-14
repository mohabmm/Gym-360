using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.AdminUsers;

public sealed record GetAdminUserByIdQuery(Guid UserId) : IQuery<AdminUserDto>;
