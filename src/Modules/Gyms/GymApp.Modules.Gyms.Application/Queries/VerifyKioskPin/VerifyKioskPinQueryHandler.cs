using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.VerifyKioskPin;

internal sealed class VerifyKioskPinQueryHandler : IQueryHandler<VerifyKioskPinQuery, bool>
{
    private readonly IGymRepository _gymRepository;
    private readonly IKioskPinHasher _hasher;

    public VerifyKioskPinQueryHandler(IGymRepository gymRepository, IKioskPinHasher hasher)
    {
        _gymRepository = gymRepository;
        _hasher = hasher;
    }

    public async Task<Result<bool>> Handle(VerifyKioskPinQuery request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<bool>(GymErrors.NotFound);

        // No PIN set → kiosk exits freely.
        if (gym.KioskPinHash is null)
            return Result.Success(true);

        return Result.Success(_hasher.Verify(request.Pin, gym.KioskPinHash));
    }
}
