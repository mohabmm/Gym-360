using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.AddMember;

public sealed record AddMemberCommand(
    Guid GymId,
    Guid UserId,
    string? Section,
    string? MemberNumber) : ICommand;
