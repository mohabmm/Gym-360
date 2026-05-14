using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

public sealed record UpdateAdminUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Phone) : ICommand;
