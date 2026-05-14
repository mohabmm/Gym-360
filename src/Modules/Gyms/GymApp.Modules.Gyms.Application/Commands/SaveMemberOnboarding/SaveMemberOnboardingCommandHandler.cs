using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.SaveMemberOnboarding;

internal sealed class SaveMemberOnboardingCommandHandler : ICommandHandler<SaveMemberOnboardingCommand, Guid>
{
    private readonly IMemberOnboardingRepository _onboardingRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public SaveMemberOnboardingCommandHandler(
        IMemberOnboardingRepository onboardingRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _onboardingRepository = onboardingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SaveMemberOnboardingCommand request, CancellationToken cancellationToken)
    {
        var existing = await _onboardingRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                request.MedicalHistory,
                request.FitnessGoals,
                request.Injuries,
                request.Medications,
                request.FitnessLevel);

            _onboardingRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(existing.Id);
        }

        var onboarding = MemberOnboarding.Create(
            request.GymId,
            request.MemberId,
            request.MedicalHistory,
            request.FitnessGoals,
            request.Injuries,
            request.Medications,
            request.FitnessLevel);

        await _onboardingRepository.AddAsync(onboarding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(onboarding.Id);
    }
}
