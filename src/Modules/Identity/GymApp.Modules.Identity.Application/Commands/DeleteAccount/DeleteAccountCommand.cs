using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.DeleteAccount;

public sealed record DeleteAccountCommand(Guid UserId) : ICommand;
