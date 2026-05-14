using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.RejectGym;

public sealed record RejectGymCommand(Guid GymId, string Reason) : ICommand;
