using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.ReactivateGym;

public sealed record ReactivateGymCommand(Guid GymId) : ICommand;
