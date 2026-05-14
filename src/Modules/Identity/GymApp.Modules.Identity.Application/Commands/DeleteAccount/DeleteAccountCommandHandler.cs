using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Identity.Application.Commands.DeleteAccount;

internal sealed class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        // PDPL: Soft delete with 30-day retention period.
        // The user is marked as deleted but data is retained for 30 days
        // before permanent purge (handled by a scheduled job in Infrastructure).
        user.MarkDeleted();
        user.RevokeAllRefreshTokens();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish integration event so other modules can anonymize/delete user data
        await _publisher.Publish(
            new AccountDeletionRequestedIntegrationEvent(request.UserId, DateTime.UtcNow.AddDays(30)),
            cancellationToken);

        return Result.Success();
    }
}
