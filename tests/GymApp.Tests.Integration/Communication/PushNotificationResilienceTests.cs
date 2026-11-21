using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymApp.Tests.Integration.Communication;

/// <summary>
/// Push is best-effort: when FCM is enabled but misconfigured (no service-account
/// key), SendAsync must log and return rather than fail the business request.
/// </summary>
public sealed class PushNotificationResilienceTests
{
    private sealed class OneTokenStore : IFcmTokenStore
    {
        public Task<IReadOnlyList<string>> GetTokensForUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(["device-token"]);

        public Task RegisterTokenAsync(Guid userId, string token, string platform, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RemoveTokenAsync(Guid userId, string token, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private static PushNotificationService EnabledWithoutCredentials()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fcm:IsEnabled"] = "true",
                ["Fcm:ProjectId"] = "gym360-app"
            })
            .Build();

        return new PushNotificationService(
            new OneTokenStore(), config, new HttpClient(), NullLogger<PushNotificationService>.Instance);
    }

    [Fact]
    public async Task SendAsync_MissingServiceAccount_DoesNotThrow()
    {
        var act = () => EnabledWithoutCredentials().SendAsync(Guid.NewGuid(), new PushContent("t", "b"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_Cancelled_StillPropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => EnabledWithoutCredentials().SendAsync(Guid.NewGuid(), new PushContent("t", "b"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
