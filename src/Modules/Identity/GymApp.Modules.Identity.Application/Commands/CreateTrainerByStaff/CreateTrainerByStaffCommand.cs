using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;

public sealed record CreateTrainerByStaffCommand(
    Guid GymId,
    string FirstName,
    string LastName,
    string Phone,
    string? Email) : ICommand<Guid>;
