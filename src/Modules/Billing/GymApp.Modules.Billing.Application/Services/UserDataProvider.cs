using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISessionPackageRepository _sessionPackageRepository;

    public UserDataProvider(
        IPaymentRepository paymentRepository,
        ISessionPackageRepository sessionPackageRepository)
    {
        _paymentRepository = paymentRepository;
        _sessionPackageRepository = sessionPackageRepository;
    }

    public string ModuleName => "Billing";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Payment and session package data is gym-scoped (requires gymId).
        // Return a summary indicating the data categories available.
        return await Task.FromResult<object>(new
        {
            UserId = userId,
            Note = "Payment and session package data is gym-scoped. Use the Billing endpoints with X-Gym-Id header to retrieve per-gym data.",
            DataCategories = new[] { "Payments", "MembershipPlans", "SessionPackages" }
        });
    }
}
