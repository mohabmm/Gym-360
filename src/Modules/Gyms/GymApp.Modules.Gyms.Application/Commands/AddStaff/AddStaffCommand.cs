using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.AddStaff;

public sealed record AddStaffCommand(
    Guid GymId,
    Guid UserId,
    string Role) : ICommand;
