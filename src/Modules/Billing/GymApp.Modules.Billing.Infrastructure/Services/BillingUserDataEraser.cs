using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Services;

/// <summary>
/// Only package-interest leads are deleted. Payments, trainer earnings, session packages
/// and their usage are financial records kept for 7 years (PDPL) and never deleted
/// (FOUNDER Q9: void only). They hold no name/phone/email — once the Identity user row is
/// gone the remaining id is an anonymous reference.
/// </summary>
internal sealed class BillingUserDataEraser : IUserDataEraser
{
    private readonly BillingDbContext _db;

    public BillingUserDataEraser(BillingDbContext db) => _db = db;

    public string ModuleName => "Billing";

    public Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.PackageInterests
            .Where(i => i.MemberId == userId || i.TrainerId == userId)
            .ExecuteDeleteAsync(cancellationToken);
}
