using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

public sealed record DeleteAdminUserCommand(Guid UserId, Guid? RequestedBy) : ICommand;
