namespace GymApp.Modules.Billing.Application.DTOs;

/// <summary>
/// Money representation for trainee-facing responses, matching the PRD shape
/// `{ amount, currency }`. Kept separate from the existing flat DTOs so
/// server-facing and staff endpoints are not disturbed by this UX change.
/// </summary>
public sealed record MoneyDto(decimal Amount, string Currency);
