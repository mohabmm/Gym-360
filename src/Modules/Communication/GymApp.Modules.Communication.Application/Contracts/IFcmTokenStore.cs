namespace GymApp.Modules.Communication.Application.Contracts;

public interface IFcmTokenStore
{
    Task<IReadOnlyList<string>> GetTokensForUserAsync(Guid userId, CancellationToken ct = default);
    Task RegisterTokenAsync(Guid userId, string token, string platform, CancellationToken ct = default);
    Task RemoveTokenAsync(Guid userId, string token, CancellationToken ct = default);
}
