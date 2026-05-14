using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Commands.SetWaterTarget;

internal sealed class SetWaterTargetCommandHandler : ICommandHandler<SetWaterTargetCommand>
{
    private readonly IWaterLogRepository _waterLogRepository;
    private readonly INutritionUnitOfWork _unitOfWork;

    public SetWaterTargetCommandHandler(
        IWaterLogRepository waterLogRepository,
        INutritionUnitOfWork unitOfWork)
    {
        _waterLogRepository = waterLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetWaterTargetCommand request, CancellationToken cancellationToken)
    {
        var waterLog = await _waterLogRepository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        if (waterLog is null)
        {
            waterLog = WaterLog.Create(request.UserId, request.GymId, request.Date, request.TargetMl);
            await _waterLogRepository.AddAsync(waterLog, cancellationToken);
        }
        else
        {
            var result = waterLog.SetTarget(request.TargetMl);
            if (result.IsFailure)
                return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
