using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

public sealed record CreateAdminUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string AdminRole,
    Guid? CreatedBy) : ICommand<Guid>;
