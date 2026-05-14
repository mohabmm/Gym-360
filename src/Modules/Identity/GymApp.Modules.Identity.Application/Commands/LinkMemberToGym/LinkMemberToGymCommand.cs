using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Application.Commands.LinkMemberToGym;

public sealed record LinkMemberToGymCommand(
    Guid UserId,
    Guid GymId,
    UserRole Role,
    string? Section,
    string? MemberNumber) : ICommand;
