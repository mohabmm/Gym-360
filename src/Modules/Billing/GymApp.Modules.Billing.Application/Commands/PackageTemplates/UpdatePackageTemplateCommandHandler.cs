using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.PackageTemplates;

internal sealed class UpdatePackageTemplateCommandHandler : ICommandHandler<UpdatePackageTemplateCommand>
{
    private readonly ISessionPackageTemplateRepository _templateRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public UpdatePackageTemplateCommandHandler(
        ISessionPackageTemplateRepository templateRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePackageTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
            return Result.Failure(new Error("Billing.PackageTemplateNotFound", "Package template not found."));

        var name = request.Name ?? template.Name;
        var sessionCount = request.SessionCount ?? template.SessionCount;
        var pricePerSession = request.PricePerSession.HasValue
            ? Money.Create(request.PricePerSession.Value)
            : template.PricePerSession;
        var totalPrice = Money.Create(pricePerSession.Amount * sessionCount);

        template.Update(name, sessionCount, pricePerSession, totalPrice);

        if (request.IsActive.HasValue)
        {
            if (!request.IsActive.Value) template.Deactivate();
        }

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
