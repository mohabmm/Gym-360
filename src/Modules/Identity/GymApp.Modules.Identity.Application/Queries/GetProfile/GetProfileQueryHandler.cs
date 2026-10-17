using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Queries.GetProfile;

internal sealed class GetProfileQueryHandler : IQueryHandler<GetProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDisplayLookup _userDisplayLookup;
    private readonly AssignedTrainerResolver _assignedTrainerResolver;

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        IUserDisplayLookup userDisplayLookup,
        AssignedTrainerResolver assignedTrainerResolver)
    {
        _userRepository = userRepository;
        _userDisplayLookup = userDisplayLookup;
        _assignedTrainerResolver = assignedTrainerResolver;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound(request.UserId));

        // Trainers-by-gym resolved only for trainee memberships — owners/staff/trainers
        // don't have a "my assigned trainer" concept in the UI.
        var traineeGymIds = user.GymMemberships
            .Where(m => m.IsActive && m.Role == UserRole.Trainee)
            .Select(m => m.GymId)
            .ToList();

        var trainersByGym = traineeGymIds.Count > 0
            ? await _assignedTrainerResolver.ResolveAcrossGymsAsync(user.Id, traineeGymIds, cancellationToken)
            : new Dictionary<Guid, Guid>();

        var trainerDisplays = trainersByGym.Count > 0
            ? await _userDisplayLookup.GetDisplayNamesAsync(trainersByGym.Values.Distinct(), cancellationToken)
            : new Dictionary<Guid, UserDisplay>();

        var gymMemberships = user.GymMemberships
            .Select(m =>
            {
                Guid? trainerId = trainersByGym.TryGetValue(m.GymId, out var tid) ? tid : null;
                string? trainerName = trainerId is not null
                                      && trainerDisplays.TryGetValue(trainerId.Value, out var display)
                    ? display.FullName
                    : null;

                return new GymMembershipDto(
                    m.GymId,
                    m.Role.ToString(),
                    m.JoinedAt,
                    m.IsActive,
                    m.Section,
                    m.MemberNumber,
                    trainerId,
                    trainerName);
            })
            .ToList();

        var response = new UserProfileResponse(
            user.Id,
            user.Email.Value,
            user.PhoneNumber.Value,
            user.FirstName,
            user.LastName,
            user.ProfilePhotoUrl,
            user.DateOfBirth?.ToString("yyyy-MM-dd"),
            user.Gender.ToString(),
            user.Role.ToString(),
            user.Status.ToString(),
            user.Language.ToString(),
            gymMemberships);

        return Result.Success(response);
    }
}
