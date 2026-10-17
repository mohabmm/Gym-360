using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

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

        var details = new OnboardingDetails
        {
            MedicalHistory = request.MedicalHistory,
            FitnessGoals = request.FitnessGoals,
            Injuries = request.Injuries,
            Medications = request.Medications,
            FitnessLevel = request.FitnessLevel
        };

        if (existing is not null)
        {
            existing.Update(details);

            _onboardingRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(existing.Id);
        }

        var onboarding = MemberOnboarding.Create(request.GymId, request.MemberId, details);

        await _onboardingRepository.AddAsync(onboarding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(onboarding.Id);
    }
}
