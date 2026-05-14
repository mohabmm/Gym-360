using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.CreateMemberByStaff;

internal sealed class CreateMemberByStaffCommandHandler : ICommandHandler<CreateMemberByStaffCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateMemberByStaffCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateMemberByStaffCommand request, CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(NormalizePhone(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<Guid>(phoneResult.Error);

        // Staff-created members get a known default password matching the seed
        // convention so trainees can log in immediately. Production should swap
        // for OTP/SMS issuance.
        const string defaultPassword = "123456";
        var defaultHash = _passwordHasher.Hash(defaultPassword);

        // If a user already exists with this phone, only reuse them when their
        // role is Trainee (the cross-gym member scenario). Reusing an account
        // that belongs to an Owner / Admin / Staff / Trainer would silently
        // reset their password to the staff-create default — a critical
        // privilege/security bug.
        var existingByPhone = await _userRepository.GetByPhoneAsync(phoneResult.Value, cancellationToken);
        if (existingByPhone is not null)
        {
            if (existingByPhone.Role != UserRole.Trainee)
                return Result.Failure<Guid>(UserErrors.PhoneReservedForOtherRole);
            await EnsureUsableAsync(existingByPhone, defaultHash, cancellationToken);
            return Result.Success(existingByPhone.Id);
        }

        var emailInput = string.IsNullOrWhiteSpace(request.Email)
            ? SynthesizePlaceholderEmail(phoneResult.Value.Value)
            : request.Email.Trim();

        var emailResult = Email.Create(emailInput);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        var existingByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (existingByEmail is not null)
        {
            if (existingByEmail.Role != UserRole.Trainee)
                return Result.Failure<Guid>(UserErrors.EmailReservedForOtherRole);
            await EnsureUsableAsync(existingByEmail, defaultHash, cancellationToken);
            return Result.Success(existingByEmail.Id);
        }

        var passwordHash = defaultHash;

        var user = DomainEntities.User.Create(
            emailResult.Value,
            phoneResult.Value,
            passwordHash,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            Gender.Male,
            UserRole.Trainee);

        user.Activate();

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }

    private async Task EnsureUsableAsync(DomainEntities.User user, string defaultHash, CancellationToken ct)
    {
        var changed = false;
        if (user.Status == UserStatus.PendingVerification)
        {
            user.Activate();
            changed = true;
        }
        if (user.Status != UserStatus.Suspended && user.Status != UserStatus.Deleted)
        {
            user.UpdatePasswordHash(defaultHash);
            changed = true;
        }
        if (changed)
        {
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

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
        return $"member.{digits}@placeholder.gymapp.local";
    }
}
