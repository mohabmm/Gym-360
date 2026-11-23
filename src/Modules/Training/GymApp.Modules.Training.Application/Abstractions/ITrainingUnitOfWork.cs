using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Application.Abstractions;

public interface ITrainingUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Runs <paramref name="work"/> in one database transaction, for steps that must be saved in
    /// order (e.g. end the old active plan before inserting the new one: the unique "one active
    /// plan per trainee" index is checked per statement).
    /// </summary>
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);
}
