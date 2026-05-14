using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Deploy;

internal sealed class DeployChangesCommandHandler : ICommandHandler<DeployChangesCommand, DeployResult>
{
    private readonly IPlanChangeLogRepository _changeLogRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public DeployChangesCommandHandler(
        IPlanChangeLogRepository changeLogRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _changeLogRepository = changeLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeployResult>> Handle(DeployChangesCommand request, CancellationToken cancellationToken)
    {
        int affectedGyms = request.Scope.ToLowerInvariant() switch
        {
            "gym" => request.TargetId.HasValue ? 1 : 0,
            "plan" => -1, // Infrastructure layer will resolve actual count
            "all" => -1,  // Infrastructure layer will resolve actual count
            _ => 0
        };

        var log = PlanChangeLog.Create(
            "Deploy",
            request.DeployedBy,
            request.TargetId,
            "Deploy",
            request.TargetId,
            newValue: request.Scope,
            reason: $"Deploy triggered for scope: {request.Scope}");

        await _changeLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // FCM + SignalR push will be handled by Infrastructure/Presentation layer
        // via domain event or direct notification service call

        var result = new DeployResult(
            request.Scope,
            request.TargetId,
            affectedGyms,
            DateTime.UtcNow);

        return Result.Success(result);
    }
}
