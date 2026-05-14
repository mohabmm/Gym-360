using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Features;

internal sealed class UpdateFeatureCommandHandler : ICommandHandler<UpdateFeatureCommand>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public UpdateFeatureCommandHandler(
        IFeatureRepository featureRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.FeatureId, cancellationToken);

        if (feature is null)
        {
            return Result.Failure(BillingErrors.FeatureNotFound);
        }

        feature.Update(
            request.Name ?? feature.Name,
            request.Description ?? feature.Description,
            request.Category ?? feature.Category,
            feature.SortOrder);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                feature.Activate();
            else
                feature.Deactivate();
        }

        _featureRepository.Update(feature);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
