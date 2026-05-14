using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.Owners;

public sealed record CreateGymOwnerResult(Guid UserId, Guid OwnerId);

public sealed record CreateGymOwnerCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone,
    string CompanyName,
    string BillingEmail,
    string PlanTier,
    string DashboardMode) : ICommand<CreateGymOwnerResult>;
