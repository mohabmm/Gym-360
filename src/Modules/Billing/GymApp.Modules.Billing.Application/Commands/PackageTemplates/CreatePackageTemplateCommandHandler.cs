using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

internal sealed class CreatePackageTemplateCommandHandler : ICommandHandler<CreatePackageTemplateCommand, Guid>
{
    private readonly ISessionPackageTemplateRepository _templateRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IBillingUnitOfWork _unitOfWork;

    public CreatePackageTemplateCommandHandler(
        ISessionPackageTemplateRepository templateRepository,
        IGymPlanService gymPlanService,
        IBillingUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePackageTemplateCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "session_packages", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("session_packages", currentPlan, "Pro"));
        }

        var pricePerSession = Money.Create(request.PricePerSession);
        var totalPrice = Money.Create(request.PricePerSession * request.SessionCount);

        var template = SessionPackageTemplate.Create(
            request.GymId,
            request.Name,
            request.SessionCount,
            pricePerSession,
            totalPrice);

        await _templateRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(template.Id);
    }
}
