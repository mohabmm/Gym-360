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

        var userResult = await ResolveStaffUserAsync(request, emailResult.Value, cancellationToken);
        if (userResult.IsFailure)
            return Result.Failure<Guid>(userResult.Error);

        var user = userResult.Value;

        var linkResult = user.LinkToGym(request.GymId, UserRole.GymStaff, section: null, memberNumber: null);
        if (linkResult.IsFailure)
            return Result.Failure<Guid>(linkResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }

    private async Task<Result<DomainEntities.User>> ResolveStaffUserAsync(
        CreateGymStaffCommand request,
        Email email,
        CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);

        return existing is not null
            ? ReuseExistingStaff(existing)
            : await CreateStaffUserAsync(request, email, cancellationToken);
    }

    private static Result<DomainEntities.User> ReuseExistingStaff(DomainEntities.User existing)
    {
        // Reuse only when the account is already a GymStaff (cross-gym
        // staff transfer). Reusing an Owner / Admin / Trainer / Trainee
        // would silently absorb someone else's identity into a staff role.
        if (existing.Role != UserRole.GymStaff)
            return Result.Failure<DomainEntities.User>(UserErrors.EmailReservedForOtherRole);
        if (existing.GymMemberships.Any(m => m.IsActive))
            return Result.Failure<DomainEntities.User>(new Error("User.AlreadyHasActiveMembership", "This user already has an active gym membership. Each user may belong to one gym at a time."));
        return Result.Success(existing);
    }

    private async Task<Result<DomainEntities.User>> CreateStaffUserAsync(
        CreateGymStaffCommand request,
        Email email,
        CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.NormalizeOrPlaceholder(request.Phone, request.Email));
        if (phoneResult.IsFailure)
            return Result.Failure<DomainEntities.User>(phoneResult.Error);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = DomainEntities.User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = phoneResult.Value,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = Gender.Male
            },
            UserRole.GymStaff);

        user.Activate();
        await _userRepository.AddAsync(user, cancellationToken);
        return Result.Success(user);
    }
}
