using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

public sealed record CreateGymStaffCommand(
    Guid GymId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone) : ICommand<Guid>;
