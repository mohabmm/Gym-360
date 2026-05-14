using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class AssignSessionPackageCommandHandler : ICommandHandler<AssignSessionPackageCommand, Guid>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly ISessionPackageTemplateRepository _templateRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IBillingUnitOfWork _unitOfWork;

    public AssignSessionPackageCommandHandler(
        ISessionPackageRepository packageRepository,
        ISessionPackageTemplateRepository templateRepository,
        IGymPlanService gymPlanService,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _templateRepository = templateRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "session_packages", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("session_packages", currentPlan, "Pro"));
        }

        var template = await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure<Guid>(BillingErrors.TemplateNotFound);
        }

        var package = SessionPackage.Create(
            request.GymId,
            request.TrainerId,
            request.MemberId,
            request.TemplateId,
            template.Name,
            template.SessionCount,
            template.PricePerSession,
            template.TotalPrice,
            request.AssignedBy,
            request.ExpiresAt);

        await _packageRepository.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(package.Id);
    }
}
