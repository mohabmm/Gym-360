using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.Repositories;

public interface IOtpCodeRepository
{
    Task<OtpCode?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<int> GetCountInWindowAsync(Guid userId, OtpPurpose purpose, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
}
