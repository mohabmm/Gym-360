using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
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

    private const string DefaultTrainerPassword = "123456";

    private void EnqueueTrainerLinked(Guid userId, Guid gymId) =>
        _integrationEvents.Enqueue(new UserLinkedToGymIntegrationEvent(
            userId, gymId, UserRole.Trainer.ToString()));

    public async Task<Result<Guid>> Handle(CreateTrainerByStaffCommand request, CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(NormalizePhone(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<Guid>(phoneResult.Error);

        // Reuse only if existing user is already a Trainer (cross-gym scenario).
        // Reusing an Owner / Admin / Staff / Trainee account would silently
        // hand the caller someone else's identity to enroll as trainer.
        var existingByPhone = await _userRepository.GetByPhoneAsync(phoneResult.Value, cancellationToken);
        if (existingByPhone is not null)
            return await ReuseTrainerAsync(existingByPhone, UserErrors.PhoneReservedForOtherRole, request.GymId, cancellationToken);

        var emailResult = Email.Create(ResolveEmail(request.Email, phoneResult.Value.Value));
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        var existingByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (existingByEmail is not null)
            return await ReuseTrainerAsync(existingByEmail, UserErrors.EmailReservedForOtherRole, request.GymId, cancellationToken);

        var passwordHash = _passwordHasher.Hash(DefaultTrainerPassword);

        var user = DomainEntities.User.Create(
            emailResult.Value,
            phoneResult.Value,
            passwordHash,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            Gender.Male,
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

    private Task<Result<Guid>> ReuseTrainerAsync(
        DomainEntities.User existing, Error reservedError, Guid gymId, CancellationToken ct) =>
        existing.Role == UserRole.Trainer
            ? EnsureLinkedAsync(existing, gymId, ct)
            : Task.FromResult(Result.Failure<Guid>(reservedError));

    private async Task<Result<Guid>> EnsureLinkedAsync(DomainEntities.User user, Guid gymId, CancellationToken ct)
    {
        // Match member-create behavior: reset password to the documented default so the
        // trainer can log in with the same `initialPassword` the endpoint advertises.
        // Staff can only call this for unclaimed/legitimate trainers (role-guarded above).
        user.UpdatePasswordHash(_passwordHasher.Hash(DefaultTrainerPassword));

        var newlyLinked = !IsActiveTrainerAt(user, gymId);
        if (newlyLinked)
        {
            var link = user.LinkToGym(gymId, UserRole.Trainer, section: null, memberNumber: null);
            if (link.IsFailure)
                return Result.Failure<Guid>(link.Error);
        }

        _userRepository.Update(user);
        if (newlyLinked)
            EnqueueTrainerLinked(user.Id, gymId);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(user.Id);
    }

    private static bool IsActiveTrainerAt(DomainEntities.User user, Guid gymId) =>
        user.GymMemberships.Any(m => m.GymId == gymId && m.Role == UserRole.Trainer && m.IsActive);

    private static string ResolveEmail(string? requested, string e164Phone) =>
        string.IsNullOrWhiteSpace(requested)
            ? SynthesizePlaceholderEmail(e164Phone)
            : requested.Trim();

    private static string NormalizePhone(string raw)
    {
        var trimmed = raw.Trim();
        var digits = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (digits.StartsWith("+")) return digits;
        if (digits.StartsWith("00")) return "+" + digits[2..];
        if (digits.Length == 11 && digits.StartsWith("01")) return "+20" + digits[1..];
        if (digits.Length == 10 && digits.StartsWith("1")) return "+20" + digits;
        return "+" + digits;
    }

    private static string SynthesizePlaceholderEmail(string e164Phone)
    {
        var digits = new string(e164Phone.Where(char.IsDigit).ToArray());
        return $"trainer.{digits}@placeholder.gymapp.local";
    }
}
