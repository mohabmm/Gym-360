using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateMember;

public sealed record UpdateMemberCommand(
    Guid GymId,
    Guid MemberId,
    string? Name,
    string? Phone,
    string? Section) : ICommand;
