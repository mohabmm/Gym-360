using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Features;

internal sealed class CreateFeatureCommandHandler : ICommandHandler<CreateFeatureCommand, Guid>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public CreateFeatureCommandHandler(
        IFeatureRepository featureRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        var existing = await _featureRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(BillingErrors.FeatureCodeAlreadyExists);
        }

        var feature = Feature.Create(new FeatureDetails
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category
        });

        await _featureRepository.AddAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(feature.Id);
    }
}
