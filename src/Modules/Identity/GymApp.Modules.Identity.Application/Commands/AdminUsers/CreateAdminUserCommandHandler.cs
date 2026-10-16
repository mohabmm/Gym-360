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

internal sealed class CreateAdminUserCommandHandler : ICommandHandler<CreateAdminUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAdminUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        if (await _userRepository.ExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<Guid>(UserErrors.EmailAlreadyExists);

        // Admin creation doesn't require phone. We store a deterministic placeholder so the
        // NOT NULL column is satisfied; the admin replaces it via profile update if needed.
        var placeholderPhone = "+999" + Math.Abs(request.Email.GetHashCode()).ToString().PadLeft(10, '0')[..10];
        var phoneResult = PhoneNumber.Create(placeholderPhone);
        if (phoneResult.IsFailure)
            return Result.Failure<Guid>(phoneResult.Error);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = DomainEntities.User.Create(
            new NewUserDetails
            {
                Email = emailResult.Value,
                PhoneNumber = phoneResult.Value,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = Gender.Male
            },
            UserRole.PlatformAdmin);

        user.Activate();
        await _userRepository.AddAsync(user, cancellationToken);

        var adminRole = ParseAdminRole(request.AdminRole);
        var platformAdmin = DomainEntities.PlatformAdmin.Create(user.Id, adminRole, request.CreatedBy);
        await _userRepository.AddPlatformAdminAsync(platformAdmin, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }

    private static AdminRole ParseAdminRole(string raw) => raw switch
    {
        "super_admin" => AdminRole.SuperAdmin,
        "admin" => AdminRole.Admin,
        "support" => AdminRole.Support,
        "analyst" => AdminRole.Analyst,
        _ => AdminRole.Admin,
    };
}
