using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Common;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.IntegrationEvents;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;

internal sealed class CreateTrainerByStaffCommandHandler : ICommandHandler<CreateTrainerByStaffCommand, Guid>
{
    // Matches member-create: staff-created trainers log in with the documented
    // `initialPassword` the endpoint advertises.
    private const string DefaultTrainerPassword = "123456";

    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public CreateTrainerByStaffCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IIntegrationEventPublisher integrationEvents)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _integrationEvents = integrationEvents;
    }

    private void EnqueueTrainerLinked(Guid userId, Guid gymId) =>
        _integrationEvents.Enqueue(new UserLinkedToGymIntegrationEvent(
            userId, gymId, UserRole.Trainer.ToString()));

    public async Task<Result<Guid>> Handle(CreateTrainerByStaffCommand request, CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.Normalize(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<Guid>(phoneResult.Error);

        // Reuse only if existing user is already a Trainer (cross-gym scenario).
        // Reusing an Owner / Admin / Staff / Trainee account would silently
        // hand the caller someone else's identity to enroll as trainer.
        var existingByPhone = await _userRepository.GetByPhoneAsync(phoneResult.Value, cancellationToken);
        if (existingByPhone is not null)
            return await ReuseExistingAsync(existingByPhone, UserErrors.PhoneReservedForOtherRole, request.GymId, cancellationToken);

        var emailInput = string.IsNullOrWhiteSpace(request.Email)
            ? SynthesizePlaceholderEmail(phoneResult.Value.Value)
            : request.Email.Trim();

        var emailResult = Email.Create(emailInput);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        var existingByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (existingByEmail is not null)
            return await ReuseExistingAsync(existingByEmail, UserErrors.EmailReservedForOtherRole, request.GymId, cancellationToken);

        var passwordHash = _passwordHasher.Hash(DefaultTrainerPassword);

        var user = DomainEntities.User.Create(
            new NewUserDetails
            {
                Email = emailResult.Value,
                PhoneNumber = phoneResult.Value,
                PasswordHash = passwordHash,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Gender = Gender.Male
            },
            UserRole.Trainer);

        user.Activate();

        var linkResult = user.LinkToGym(request.GymId, UserRole.Trainer, section: null, memberNumber: null);
        if (linkResult.IsFailure)
            return Result.Failure<Guid>(linkResult.Error);

        await _userRepository.AddAsync(user, cancellationToken);
        EnqueueTrainerLinked(user.Id, request.GymId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }

    private async Task<Result<Guid>> ReuseExistingAsync(
        DomainEntities.User existing, Error reservedForOtherRole, Guid gymId, CancellationToken ct) =>
        existing.Role == UserRole.Trainer
            ? await EnsureLinkedAsync(existing, gymId, ct)
            : Result.Failure<Guid>(reservedForOtherRole);

    private async Task<Result<Guid>> EnsureLinkedAsync(DomainEntities.User user, Guid gymId, CancellationToken ct)
    {
        // Reset to the default password so the trainer can log in with the advertised
        // `initialPassword`. Only reachable for existing trainers (role-guarded above).
        user.UpdatePasswordHash(_passwordHasher.Hash(DefaultTrainerPassword));

        if (!IsActiveTrainerAt(user, gymId))
        {
            var link = user.LinkToGym(gymId, UserRole.Trainer, section: null, memberNumber: null);
            if (link.IsFailure)
                return Result.Failure<Guid>(link.Error);
            EnqueueTrainerLinked(user.Id, gymId);
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(user.Id);
    }

    private static bool IsActiveTrainerAt(DomainEntities.User user, Guid gymId) =>
        user.GymMemberships.Any(m => m.IsActive && m.Role == UserRole.Trainer && m.GymId == gymId);


    private static string SynthesizePlaceholderEmail(string e164Phone)
    {
        var digits = new string(e164Phone.Where(char.IsDigit).ToArray());
        return $"trainer.{digits}@placeholder.gymapp.local";
    }
}
