using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Commands.CreateMealPlan;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Tests.Application.Nutrition;

public class CreateMealPlanCommandHandlerTests
{
    private readonly IMealPlanRepository _mealPlanRepository = Substitute.For<IMealPlanRepository>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly INutritionUnitOfWork _unitOfWork = Substitute.For<INutritionUnitOfWork>();
    private readonly CreateMealPlanCommandHandler _sut;

    public CreateMealPlanCommandHandlerTests()
    {
        _sut = new CreateMealPlanCommandHandler(_mealPlanRepository, _gymPlanService, _unitOfWork);
    }

    private static CreateMealPlanCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            TrainerId: Guid.NewGuid(),
            GymId: gymId ?? Guid.NewGuid(),
            Name: "Weight Loss Plan",
            Description: "Low calorie plan",
            DailyCalorieTarget: 2000,
            MacroTargets: new MacroTargetsDto(150, 200, 65),
            Days: new List<MealPlanDayDto>
            {
                new(DayOfWeek.Monday, new List<PlannedMealDto>
                {
                    new("Breakfast", "Oatmeal", "With fruit", new List<FoodItemDto>
                    {
                        new("Oats", 100, "g", 350, 12, 60, 7)
                    }, 350, 12, 60, 7)
                })
            },
            IsTemplate: false);

    [Fact]
    public async Task Handle_FeatureEnabled_CreatesMealPlan()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "meal_plans", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _mealPlanRepository.Received(1).AddAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "meal_plans", Arg.Any<CancellationToken>())
            .Returns(false);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("free");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _mealPlanRepository.DidNotReceive().AddAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
    }
}
