import 'package:freezed_annotation/freezed_annotation.dart';
import 'food_item_model.dart';

part 'meal_plan_model.freezed.dart';
part 'meal_plan_model.g.dart';

@freezed
class MealPlanModel with _$MealPlanModel {
  const factory MealPlanModel({
    required String id,
    // Legacy: kept for existing callers. The trainee /me endpoint populates
    // [assignedByTrainerId] / [assignedByTrainerName] instead. Either pair
    // may be present depending on the endpoint.
    @Default('') String trainerId,
    String? trainerName,
    @Default('') String traineeId,
    String? traineeName,
    String? name,
    String? description,
    @Default(<MealPlanDayModel>[]) List<MealPlanDayModel> days,
    int? totalCalories,
    double? totalProtein,
    double? totalCarbs,
    double? totalFat,
    DateTime? startDate,
    DateTime? endDate,
    @Default('') String status,
    DateTime? createdAt,
    // PRD /me/meal-plan fields.
    String? assignedByTrainerId,
    String? assignedByTrainerName,
    DateTime? assignedAt,
    int? dailyCalorieTarget,
    MealPlanMacroTargetsModel? macroTargets,
    String? notes,
  }) = _MealPlanModel;

  factory MealPlanModel.fromJson(Map<String, dynamic> json) =>
      _$MealPlanModelFromJson(json);
}

/// Macro targets attached to a trainee's assigned meal plan.
/// See PRD `GET /api/v1/nutrition/me/meal-plan`.
@freezed
class MealPlanMacroTargetsModel with _$MealPlanMacroTargetsModel {
  const factory MealPlanMacroTargetsModel({
    required int proteinGrams,
    required int carbsGrams,
    required int fatGrams,
  }) = _MealPlanMacroTargetsModel;

  factory MealPlanMacroTargetsModel.fromJson(Map<String, dynamic> json) =>
      _$MealPlanMacroTargetsModelFromJson(json);
}

/// Totals over a meal plan day's planned meals.
@freezed
class MealPlanDailyTotalsModel with _$MealPlanDailyTotalsModel {
  const factory MealPlanDailyTotalsModel({
    int? calories,
    int? proteinGrams,
    int? carbsGrams,
    int? fatGrams,
  }) = _MealPlanDailyTotalsModel;

  factory MealPlanDailyTotalsModel.fromJson(Map<String, dynamic> json) =>
      _$MealPlanDailyTotalsModelFromJson(json);
}

/// Alternative meal option the trainee can swap in for a planned meal.
@freezed
class MealAlternativeModel with _$MealAlternativeModel {
  const factory MealAlternativeModel({
    String? name,
    @Default(<MealItemModel>[]) List<MealItemModel> items,
  }) = _MealAlternativeModel;

  factory MealAlternativeModel.fromJson(Map<String, dynamic> json) =>
      _$MealAlternativeModelFromJson(json);
}

@freezed
class MealPlanDayModel with _$MealPlanDayModel {
  // [dayOfWeek] is the lowercase enum name from the backend ("monday" ..
  // "sunday"); use [dayOfWeekIndex] for 0-based Monday-first indexing.
  const factory MealPlanDayModel({
    @Default('') String id,
    @Default('') String dayOfWeek,
    @Default(<MealModel>[]) List<MealModel> meals,
    int? totalCalories,
    // PRD /me/meal-plan fields.
    int? dayNumber,
    String? label,
    List<MealModel>? plannedMeals,
    MealPlanDailyTotalsModel? dailyTotals,
  }) = _MealPlanDayModel;

  factory MealPlanDayModel.fromJson(Map<String, dynamic> json) =>
      _$MealPlanDayModelFromJson(json);
}

@freezed
class MealModel with _$MealModel {
  const factory MealModel({
    @Default('') String id,
    @Default('') String mealType,
    @Default('') String name,
    String? nameAr,
    @Default(<MealItemModel>[]) List<MealItemModel> items,
    int? totalCalories,
    String? notes,
    String? time,
    // PRD /me/meal-plan: alternative meal options.
    List<MealAlternativeModel>? alternatives,
  }) = _MealModel;

  factory MealModel.fromJson(Map<String, dynamic> json) =>
      _$MealModelFromJson(json);
}

@freezed
class MealItemModel with _$MealItemModel {
  const factory MealItemModel({
    @Default('') String id,
    @Default('') String foodItemId,
    FoodItemModel? foodItem,
    @Default(0) double quantity,
    @Default('') String unit,
    int? calories,
    double? protein,
    double? carbs,
    double? fat,
    // PRD /me/meal-plan: denormalised food name + gram-suffixed macro fields.
    String? foodName,
    int? proteinGrams,
    int? carbsGrams,
    int? fatGrams,
  }) = _MealItemModel;

  factory MealItemModel.fromJson(Map<String, dynamic> json) =>
      _$MealItemModelFromJson(json);
}

@freezed
class FoodLogModel with _$FoodLogModel {
  const factory FoodLogModel({
    required String id,
    required String userId,
    required DateTime date,
    required List<FoodLogEntryModel> entries,
    int? totalCalories,
    double? totalProtein,
    double? totalCarbs,
    double? totalFat,
  }) = _FoodLogModel;

  factory FoodLogModel.fromJson(Map<String, dynamic> json) =>
      _$FoodLogModelFromJson(json);
}

@freezed
class FoodLogEntryModel with _$FoodLogEntryModel {
  const factory FoodLogEntryModel({
    required String id,
    required String mealType,
    String? foodItemId,
    FoodItemModel? foodItem,
    required String name,
    required double quantity,
    required String unit,
    required int calories,
    double? protein,
    double? carbs,
    double? fat,
    DateTime? loggedAt,
  }) = _FoodLogEntryModel;

  factory FoodLogEntryModel.fromJson(Map<String, dynamic> json) =>
      _$FoodLogEntryModelFromJson(json);
}
