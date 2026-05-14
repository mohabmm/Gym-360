using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.CreateMemberByStaff;

public sealed record CreateMemberByStaffCommand(
    string FirstName,
    string LastName,
    string Phone,
    string? Email) : ICommand<Guid>;
