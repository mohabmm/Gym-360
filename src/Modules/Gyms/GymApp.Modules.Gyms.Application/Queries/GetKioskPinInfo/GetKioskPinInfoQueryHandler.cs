using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetKioskPinInfo;

internal sealed class GetKioskPinInfoQueryHandler : IQueryHandler<GetKioskPinInfoQuery, KioskPinInfoResponse>
{
    private readonly IGymRepository _gymRepository;

    public GetKioskPinInfoQueryHandler(IGymRepository gymRepository)
    {
        _gymRepository = gymRepository;
    }

    public async Task<Result<KioskPinInfoResponse>> Handle(GetKioskPinInfoQuery request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<KioskPinInfoResponse>(GymErrors.NotFound);

        return Result.Success(new KioskPinInfoResponse(
            IsSet: gym.KioskPinHash is not null,
            Hash: gym.KioskPinHash));
    }
}
