using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.FreezeMember;

public sealed record FreezeMemberCommand(Guid GymId, Guid MemberId) : ICommand;
