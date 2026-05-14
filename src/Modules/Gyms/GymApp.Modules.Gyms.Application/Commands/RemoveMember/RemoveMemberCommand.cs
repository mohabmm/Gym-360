using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid GymId,
    Guid MemberId) : ICommand;
