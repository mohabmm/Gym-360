using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class UseSessionFromPackageCommandHandler : ICommandHandler<UseSessionFromPackageCommand>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public UseSessionFromPackageCommandHandler(
        ISessionPackageRepository packageRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UseSessionFromPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.GetByIdAsync(request.PackageId, cancellationToken);

        if (package is null)
        {
            return Result.Failure(BillingErrors.PackageNotFound);
        }

        Result result = request.UsageType switch
        {
            "LateCancelled" => package.RecordLateCancelUsage(request.SessionId),
            _ => package.UseSession(request.SessionId)
        };

        if (result.IsFailure)
        {
            return result;
        }

        _packageRepository.Update(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
