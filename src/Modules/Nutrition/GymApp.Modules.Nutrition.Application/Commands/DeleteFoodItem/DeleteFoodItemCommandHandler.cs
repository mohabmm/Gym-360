using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Commands.DeleteFoodItem;

internal sealed class DeleteFoodItemCommandHandler : ICommandHandler<DeleteFoodItemCommand>
{
    private readonly IFoodLogRepository _foodLogRepository;
    private readonly INutritionUnitOfWork _unitOfWork;

    public DeleteFoodItemCommandHandler(
        IFoodLogRepository foodLogRepository,
        INutritionUnitOfWork unitOfWork)
    {
        _foodLogRepository = foodLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteFoodItemCommand request, CancellationToken cancellationToken)
    {
        var foodLog = await _foodLogRepository.GetByMealIdAsync(request.MealEntryId, cancellationToken);

        // Treat "not mine" the same as "not found" so ownership is not leaked.
        if (foodLog is null || foodLog.UserId != request.UserId)
            return Result.Failure(NutritionErrors.FoodLogNotFound(request.MealEntryId));

        var result = foodLog.RemoveFoodItem(request.MealEntryId, request.ItemIndex);
        if (result.IsFailure)
            return result;

        // The aggregate is change-tracked (loaded without AsNoTracking), so the
        // owned-collection edit and any emptied-meal removal persist on save.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
