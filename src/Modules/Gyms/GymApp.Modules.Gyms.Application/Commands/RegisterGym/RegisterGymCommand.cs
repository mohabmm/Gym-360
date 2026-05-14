using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.RegisterGym;

public sealed record RegisterGymCommand(
    Guid OwnerId,
    string Name,
    string? Description,
    AddressDto Address,
    ContactInfoDto Contact,
    bool AutoApprove = true) : ICommand<Guid>;
