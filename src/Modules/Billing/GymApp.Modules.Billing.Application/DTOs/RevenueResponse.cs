namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record RevenueResponse(
    decimal TotalRevenue,
    string Currency,
    DateOnly From,
    DateOnly To);
