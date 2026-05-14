namespace GymApp.Common.Application.Abstractions;

public interface IUserDataProvider
{
    string ModuleName { get; }
    Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
}
