using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Common;
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
        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.Normalize(request.Phone));
        if (phoneResult.IsFailure)
            return Result.Failure<Guid>(phoneResult.Error);

        // staff-create is strictly for NEW people. If the phone/email already maps to
        // an account we must NOT reuse it: silently returning the existing user made
        // the staff-entered name/email vanish and surfaced a different person's identity
        // (the reported bug). For an existing trainee we reject with 409 + a hint to use
        // "Add existing member" (staff-link-existing). For a non-trainee we reject too,
        // to avoid clobbering an owner/admin/trainer login.
        var existingByPhone = await _userRepository.GetByPhoneAsync(phoneResult.Value, cancellationToken);
        if (existingByPhone is not null)
        {
            return Result.Failure<Guid>(existingByPhone.Role == UserRole.Trainee
                ? UserErrors.MemberPhoneAlreadyExists
                : UserErrors.PhoneReservedForOtherRole);
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
            return Result.Failure<Guid>(existingByEmail.Role == UserRole.Trainee
                ? UserErrors.MemberEmailAlreadyExists
                : UserErrors.EmailReservedForOtherRole);
        }

        // Staff-created members get a known default password matching the seed
        // convention so trainees can log in immediately. Production should swap
        // for OTP/SMS issuance.
        const string defaultPassword = "123456";
        var passwordHash = _passwordHasher.Hash(defaultPassword);

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

    private static string SynthesizePlaceholderEmail(string e164Phone)
    {
        var digits = new string(e164Phone.Where(char.IsDigit).ToArray());
        return $"member.{digits}@placeholder.gymapp.local";
    }
}
