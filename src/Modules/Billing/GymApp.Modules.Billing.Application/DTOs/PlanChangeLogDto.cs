namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PlanChangeLogDto(
    Guid Id,
    Guid? GymId,
    string ChangeType,
    string? EntityType,
    Guid? EntityId,
    string? OldValue,
    string? NewValue,
    string? Reason,
    Guid ChangedBy,
    DateTime ChangedAt);
