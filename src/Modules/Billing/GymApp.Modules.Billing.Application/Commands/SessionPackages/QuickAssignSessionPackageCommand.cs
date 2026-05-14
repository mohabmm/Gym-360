using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

/// <summary>
/// Staff-driven "assign trainer to member" flow. Caller picks a trainer +
/// session count (multiples of 4) without needing a pre-defined package
/// template. Used by the staff web/mobile member detail screen so non-billing
/// staff can attach a trainer in two clicks. TotalPrice is the lump-sum
/// price for the whole package; per-session price is derived = TotalPrice /
/// SessionCount. Leave TotalPrice null for free assignments.
/// </summary>
public sealed record QuickAssignSessionPackageCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    int SessionCount,
    Guid AssignedBy,
    decimal? TotalPrice = null,
    // When TotalPrice > 0 and PaymentMethod is supplied, a Payment row is
    // created in the same transaction with Purpose=SessionPackage and
    // ReferenceId = new package Id, so payments/earnings tables stay in sync.
    string? PaymentMethod = null,
    DateTime? ExpiresAt = null) : ICommand<Guid>;
