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

namespace GymApp.Modules.Identity.Application.Commands.Owners;

internal sealed class CreateGymOwnerCommandHandler : ICommandHandler<CreateGymOwnerCommand, CreateGymOwnerResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public CreateGymOwnerCommandHandler(
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

    public async Task<Result<CreateGymOwnerResult>> Handle(CreateGymOwnerCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<CreateGymOwnerResult>(emailResult.Error);

        if (await _userRepository.ExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<CreateGymOwnerResult>(UserErrors.EmailAlreadyExists);

        var phoneResult = PhoneNumber.Create(MemberPhoneNormalizer.NormalizeOrPlaceholder(request.Phone, request.Email));
        if (phoneResult.IsFailure)
            return Result.Failure<CreateGymOwnerResult>(phoneResult.Error);

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
            UserRole.GymOwner);

        user.Activate();
        await _userRepository.AddAsync(user, cancellationToken);

        var owner = DomainEntities.Owner.Create(user.Id, request.CompanyName, request.BillingEmail);
        await _userRepository.AddOwnerAsync(owner, cancellationToken);

        var dashboardMode = Enum.Parse<DashboardMode>(request.DashboardMode, ignoreCase: true);
        var contract = DomainEntities.Contract.Create(owner.Id, new ContractDetails
        {
            DashboardMode = dashboardMode,
            PlanTier = request.PlanTier,
            EffectiveFrom = DateTime.UtcNow
        });
        await _userRepository.AddContractAsync(contract, cancellationToken);

        user.LinkToOwner(owner.Id);

        // Auto-provision the owner's first gym so they never hit a "No owned gyms
        // found" dead-end on login. The Gyms module consumes this and registers a
        // gym owned by this user id (idempotent — skipped if they already own one).
        _integrationEvents.Enqueue(new GymOwnerCreatedIntegrationEvent(
            user.Id,
            request.CompanyName,
            request.BillingEmail,
            request.Phone));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateGymOwnerResult(user.Id, owner.Id));
    }
}
