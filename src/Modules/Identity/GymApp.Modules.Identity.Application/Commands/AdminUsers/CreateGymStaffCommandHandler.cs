using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class CreateGymStaffCommandHandler : ICommandHandler<CreateGymStaffCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateGymStaffCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateGymStaffCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        var existing = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        DomainEntities.User user;

        if (existing is not null)
        {
            // Reuse only when the account is already a GymStaff (cross-gym
            // staff transfer). Reusing an Owner / Admin / Trainer / Trainee
            // would silently absorb someone else's identity into a staff role.
            if (existing.Role != UserRole.GymStaff)
                return Result.Failure<Guid>(UserErrors.EmailReservedForOtherRole);
            user = existing;
            if (user.GymMemberships.Any(m => m.IsActive))
                return Result.Failure<Guid>(new Error("User.AlreadyHasActiveMembership", "This user already has an active gym membership. Each user may belong to one gym at a time."));
        }
        else
        {
            var phoneResult = PhoneNumber.Create(NormalizePhone(request.Phone, request.Email));
            if (phoneResult.IsFailure)
                return Result.Failure<Guid>(phoneResult.Error);

            var passwordHash = _passwordHasher.Hash(request.Password);

            user = DomainEntities.User.Create(
                new NewUserDetails
                {
                    Email = emailResult.Value,
                    PhoneNumber = phoneResult.Value,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Gender = Gender.Male
                },
                UserRole.GymStaff);

            user.Activate();
            await _userRepository.AddAsync(user, cancellationToken);
        }

        var linkResult = user.LinkToGym(request.GymId, UserRole.GymStaff, section: null, memberNumber: null);
        if (linkResult.IsFailure)
            return Result.Failure<Guid>(linkResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }

    private static string NormalizePhone(string? raw, string email)
    {
        var trimmed = raw?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmed))
        {
            var hash = Math.Abs(email.GetHashCode()).ToString().PadLeft(10, '0');
            return "+999" + hash[..Math.Min(10, hash.Length)];
        }

        var digits = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (digits.StartsWith("+")) return digits;
        if (digits.StartsWith("00")) return "+" + digits[2..];
        if (digits.Length == 11 && digits.StartsWith("01")) return "+20" + digits[1..];
        if (digits.Length == 10 && digits.StartsWith("1")) return "+20" + digits;

        return "+" + digits;
    }
}
