namespace GymApp.Common.Application.Abstractions;

/// <summary>
/// Erase side of <see cref="IUserDataProvider"/>: each module implements it to permanently
/// delete one user's personal data from its own tables. Called once per tenant scope
/// (per gym in Hard isolation) by Identity's admin hard-delete. Must be idempotent.
/// Financial records (payments, earnings, purchased packages) are retained per PDPL
/// 7-year rule / FOUNDER Q9 — implementations leave them, keyed by the orphaned id.
/// </summary>
public interface IUserDataEraser
{
    string ModuleName { get; }

    /// <summary>Returns the number of rows deleted or changed (for audit logging).</summary>
    Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default);
}
