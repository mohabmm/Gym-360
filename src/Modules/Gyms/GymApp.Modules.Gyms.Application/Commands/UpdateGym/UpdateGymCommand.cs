using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateGym;

public sealed record UpdateGymCommand(
    Guid GymId,
    string? Name,
    string? Description,
    AddressDto? Address,
    ContactInfoDto? Contact,
    List<string>? Amenities) : ICommand;
