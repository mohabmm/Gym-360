namespace GymApp.Modules.Billing.Shared;

public interface IGymPlanService
{
    Task<bool> IsFeatureEnabledAsync(Guid gymId, string featureCode, CancellationToken cancellationToken = default);
    Task<List<string>> GetEnabledFeaturesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetMemberLimitAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetTrainerLimitAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetStaffLimitAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<string> GetPlanCodeAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<long> GetStorageLimitBytesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<long> GetCurrentStorageUsedBytesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetPhotoLimitPerMemberAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetCheckInRetentionDaysAsync(Guid gymId, CancellationToken cancellationToken = default);
}
