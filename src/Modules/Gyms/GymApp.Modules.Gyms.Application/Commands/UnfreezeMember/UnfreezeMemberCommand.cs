using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UnfreezeMember;

public sealed record UnfreezeMemberCommand(Guid GymId, Guid MemberId) : ICommand;
