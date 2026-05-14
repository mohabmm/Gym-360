using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.BookClass;

public sealed record BookClassCommand(
    Guid GymId,
    Guid ClassId,
    Guid MemberId) : ICommand;
