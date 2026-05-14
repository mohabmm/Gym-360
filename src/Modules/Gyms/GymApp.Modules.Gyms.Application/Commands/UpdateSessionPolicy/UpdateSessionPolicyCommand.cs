using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSessionPolicy;

public sealed record UpdateSessionPolicyCommand(
    Guid GymId,
    int CancellationWindowHours,
    int NoshowThreshold,
    int NoshowWindowDays,
    string NoshowAction) : ICommand;
