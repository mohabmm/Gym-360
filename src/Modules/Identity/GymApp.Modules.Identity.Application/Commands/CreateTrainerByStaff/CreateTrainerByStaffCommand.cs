using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;

public sealed record CreateTrainerByStaffCommand(
    Guid GymId,
    string FirstName,
    string LastName,
    string Phone,
    string? Email) : ICommand<CreateTrainerByStaffResult>;

/// <param name="UserId">The trainer's user id (new or existing).</param>
/// <param name="InitialPassword">
/// The one-time password for a newly created account; null when the trainer already
/// had an account, which keeps its own password.
/// </param>
public sealed record CreateTrainerByStaffResult(Guid UserId, string? InitialPassword);
