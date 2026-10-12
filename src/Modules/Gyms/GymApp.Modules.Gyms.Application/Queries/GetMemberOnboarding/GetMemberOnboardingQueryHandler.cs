using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetMemberOnboarding;

internal sealed class GetMemberOnboardingQueryHandler
    : IQueryHandler<GetMemberOnboardingQuery, MemberOnboardingDto?>
{
    private readonly IMemberOnboardingRepository _onboardingRepository;

    public GetMemberOnboardingQueryHandler(IMemberOnboardingRepository onboardingRepository)
    {
        _onboardingRepository = onboardingRepository;
    }

    public async Task<Result<MemberOnboardingDto?>> Handle(
        GetMemberOnboardingQuery request,
        CancellationToken cancellationToken)
    {
        var onboarding = await _onboardingRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

        // "Not filled in yet" is a normal state for the intake form, not an error.
        if (onboarding is null)
            return Result.Success<MemberOnboardingDto?>(null);

        return Result.Success<MemberOnboardingDto?>(new MemberOnboardingDto(
            onboarding.Id,
            onboarding.GymId,
            onboarding.MemberId,
            onboarding.MedicalHistory,
            onboarding.FitnessGoals,
            onboarding.Injuries,
            onboarding.Medications,
            onboarding.FitnessLevel));
    }
}
