namespace GymApp.Common.Application.Tenancy;

public interface IOwnerDashboardService
{
    Task<OwnerDashboard> GetAsync(Guid ownerId, CancellationToken cancellationToken = default);
}

public sealed record OwnerDashboard(
    Guid OwnerId,
    int TotalGyms,
    int TotalActiveMembers,
    int TotalTrainers,
    decimal TotalRevenueLast30Days,
    int TotalSessionsLast30Days,
    IReadOnlyList<OwnerDashboardGymRow> ByGym);

public sealed record OwnerDashboardGymRow(
    Guid GymId,
    string GymName,
    string SchemaPrefix,
    int ActiveMembers,
    int Trainers,
    decimal RevenueLast30Days,
    int SessionsLast30Days);
