using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.Owners;

internal sealed class CreateGymOwnerCommandHandler : ICommandHandler<CreateGymOwnerCommand, CreateGymOwnerResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateGymOwnerCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<CreateGymOwnerResult>> Handle(CreateGymOwnerCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<CreateGymOwnerResult>(emailResult.Error);

        if (await _userRepository.ExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<CreateGymOwnerResult>(UserErrors.EmailAlreadyExists);

        var phoneResult = PhoneNumber.Create(NormalizePhone(request.Phone, request.Email));
        if (phoneResult.IsFailure)
            return Result.Failure<CreateGymOwnerResult>(phoneResult.Error);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = DomainEntities.User.Create(
            emailResult.Value,
            phoneResult.Value,
            passwordHash,
            request.FirstName,
            request.LastName,
            Gender.Male,
            UserRole.GymOwner);

        user.Activate();
        await _userRepository.AddAsync(user, cancellationToken);

        var owner = DomainEntities.Owner.Create(user.Id, request.CompanyName, request.BillingEmail);
        await _userRepository.AddOwnerAsync(owner, cancellationToken);

        var dashboardMode = Enum.Parse<DashboardMode>(request.DashboardMode, ignoreCase: true);
        var contract = DomainEntities.Contract.Create(owner.Id, dashboardMode, request.PlanTier, DateTime.UtcNow);
        await _userRepository.AddContractAsync(contract, cancellationToken);

        user.LinkToOwner(owner.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateGymOwnerResult(user.Id, owner.Id));
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
