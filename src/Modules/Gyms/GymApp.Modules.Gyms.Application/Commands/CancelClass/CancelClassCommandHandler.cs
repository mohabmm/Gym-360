using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.CancelClass;

internal sealed class CancelClassCommandHandler : ICommandHandler<CancelClassCommand>
{
    private readonly IGymClassRepository _gymClassRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public CancelClassCommandHandler(IGymClassRepository gymClassRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymClassRepository = gymClassRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelClassCommand request, CancellationToken cancellationToken)
    {
        var gymClass = await _gymClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (gymClass is null || gymClass.GymId != request.GymId)
            return Result.Failure(GymErrors.ClassNotFound);

        var result = gymClass.Cancel();
        if (result.IsFailure)
            return result;

        _gymClassRepository.Update(gymClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
