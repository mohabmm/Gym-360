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

internal sealed class CreateTrainerByStaffCommandHandler : ICommandHandler<CreateTrainerByStaffCommand, CreateTrainerByStaffResult>
{
    // New staff-created trainers log in with StaffCreatedAccount.DefaultPassword.
    // Existing trainers are never reset to it: anyone who knows it could take over
    // the account of a coach from another gym just by entering their phone.

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

    public async Task<Result<CreateTrainerByStaffResult>> Handle(CreateTrainerByStaffCommand request, CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.Normalize(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<CreateTrainerByStaffResult>(phoneResult.Error);

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
            return Result.Failure<CreateTrainerByStaffResult>(emailResult.Error);

        var existingByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (existingByEmail is not null)
            return await ReuseExistingAsync(existingByEmail, UserErrors.EmailReservedForOtherRole, request.GymId, cancellationToken);

        var passwordHash = _passwordHasher.Hash(StaffCreatedAccount.DefaultPassword);

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
            return Result.Failure<CreateTrainerByStaffResult>(linkResult.Error);

        await _userRepository.AddAsync(user, cancellationToken);
        EnqueueTrainerLinked(user.Id, request.GymId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateTrainerByStaffResult(user.Id, StaffCreatedAccount.DefaultPassword));
    }

    private async Task<Result<CreateTrainerByStaffResult>> ReuseExistingAsync(
        DomainEntities.User existing, Error reservedForOtherRole, Guid gymId, CancellationToken ct) =>
        existing.Role == UserRole.Trainer
            ? await EnsureLinkedAsync(existing, gymId, ct)
            : Result.Failure<CreateTrainerByStaffResult>(reservedForOtherRole);

    /// Links an existing trainer to this gym. Their password is left as it is, so the
    /// result carries no initial password: they keep logging in as before.
    private async Task<Result<CreateTrainerByStaffResult>> EnsureLinkedAsync(
        DomainEntities.User user, Guid gymId, CancellationToken ct)
    {
        var existing = new CreateTrainerByStaffResult(user.Id, InitialPassword: null);
        if (IsActiveTrainerAt(user, gymId))
            return Result.Success(existing);

        var link = user.LinkToGym(gymId, UserRole.Trainer, section: null, memberNumber: null);
        if (link.IsFailure)
            return Result.Failure<CreateTrainerByStaffResult>(link.Error);
        EnqueueTrainerLinked(user.Id, gymId);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(existing);
    }

    private static bool IsActiveTrainerAt(DomainEntities.User user, Guid gymId) =>
        user.GymMemberships.Any(m => m.IsActive && m.Role == UserRole.Trainer && m.GymId == gymId);


    private static string SynthesizePlaceholderEmail(string e164Phone)
    {
        var digits = new string(e164Phone.Where(char.IsDigit).ToArray());
        return $"trainer.{digits}@placeholder.gymapp.local";
    }
}
