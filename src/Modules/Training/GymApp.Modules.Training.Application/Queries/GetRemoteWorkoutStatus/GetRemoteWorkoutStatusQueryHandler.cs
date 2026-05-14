using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetRemoteWorkoutStatus;

internal sealed class GetRemoteWorkoutStatusQueryHandler : IQueryHandler<GetRemoteWorkoutStatusQuery, RemoteWorkoutStatusDto>
{
    private readonly IRemoteWorkoutConfigRepository _remoteWorkoutConfigRepository;

    public GetRemoteWorkoutStatusQueryHandler(
        IRemoteWorkoutConfigRepository remoteWorkoutConfigRepository)
    {
        _remoteWorkoutConfigRepository = remoteWorkoutConfigRepository;
    }

    public async Task<Result<RemoteWorkoutStatusDto>> Handle(
        GetRemoteWorkoutStatusQuery request, CancellationToken cancellationToken)
    {
        var config = await _remoteWorkoutConfigRepository.GetActiveByClientAsync(
            request.ClientId, request.GymId, cancellationToken);

        if (config is null)
        {
            var inactive = new RemoteWorkoutStatusDto(
                false, null, null, null, null);

            return Result.Success(inactive);
        }

        var dto = new RemoteWorkoutStatusDto(
            config.IsActive,
            config.TrainerId,
            config.ActivatedAt,
            config.DeactivatedAt,
            config.Reason);

        return Result.Success(dto);
    }
}
