namespace GymApp.Modules.Identity.Application.Abstractions;

/// <summary>
/// Runs every module's <see cref="GymApp.Common.Application.Abstractions.IUserDataEraser"/>
/// for one user in every tenant (each gym in Hard isolation). Erase counterpart of the
/// PDPL data export. Idempotent; throws if any module fails so the caller can retry.
/// </summary>
public interface IUserDataPurger
{
    Task PurgeAsync(Guid userId, CancellationToken cancellationToken = default);
}
