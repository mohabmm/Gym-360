using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.CancelClass;

public sealed record CancelClassCommand(Guid GymId, Guid ClassId) : ICommand;
