using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Common;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Application.Queries.LookupTraineeByPhone;

internal sealed class LookupTraineeByPhoneQueryHandler
    : IQueryHandler<LookupTraineeByPhoneQuery, TraineeLookupResponse>
{
    // "NotFound" suffix → ErrorStatusCodeMapper returns 404. Kept deliberately vague
    // (no "phone X belongs to nobody" leak) — staff only need to know there is no
    // existing member to re-link.
    private static readonly Error NotFound = new("User.NotFound", "No member found with this phone number.");

    private readonly IUserRepository _userRepository;

    public LookupTraineeByPhoneQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<TraineeLookupResponse>> Handle(
        LookupTraineeByPhoneQuery request,
        CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.Normalize(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<TraineeLookupResponse>(NotFound);

        var user = await _userRepository.GetByPhoneAsync(phoneResult.Value, cancellationToken);
        if (user is null || user.Role != UserRole.Trainee)
            return Result.Failure<TraineeLookupResponse>(NotFound);

        // One-Gym rule: a trainee holds at most one active membership. Surface its gym
        // (if any) so the re-link endpoint can reject an active-elsewhere trainee early.
        var activeGymId = user.GymMemberships
            .Where(m => m.IsActive && m.Role == UserRole.Trainee)
            .Select(m => (Guid?)m.GymId)
            .FirstOrDefault();

        return Result.Success(new TraineeLookupResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.PhoneNumber.Value,
            activeGymId));
    }
}
