using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.AdminUsers;

public sealed record ListUsersQuery(string? Role) : IQuery<List<AdminUserDto>>;
