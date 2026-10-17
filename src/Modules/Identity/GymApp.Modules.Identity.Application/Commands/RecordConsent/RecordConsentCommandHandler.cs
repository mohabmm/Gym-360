using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Application.Abstractions;

namespace GymApp.Modules.Identity.Application.Commands.RecordConsent;

internal sealed class RecordConsentCommandHandler : ICommandHandler<RecordConsentCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserConsentRepository _consentRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public RecordConsentCommandHandler(
        IUserRepository userRepository,
        IUserConsentRepository consentRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _consentRepository = consentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecordConsentCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        var existingConsent = await _consentRepository.GetByUserIdAndTypeAsync(
            request.UserId, request.ConsentType, cancellationToken);

        if (existingConsent is not null)
            ApplyToExisting(existingConsent, request);
        else if (request.IsGranted)
            await AddGrantedConsentAsync(request, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private void ApplyToExisting(UserConsent existingConsent, RecordConsentCommand request)
    {
        if (request.IsGranted)
            existingConsent.Grant(request.ConsentText, request.IpAddress);
        else
            existingConsent.Withdraw();

        _consentRepository.Update(existingConsent);
    }

    private Task AddGrantedConsentAsync(RecordConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = UserConsent.Create(
            request.UserId,
            request.ConsentType,
            request.ConsentText,
            request.IpAddress);

        return _consentRepository.AddAsync(consent, cancellationToken);
    }
}
