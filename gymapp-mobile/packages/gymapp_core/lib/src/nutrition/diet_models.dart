/// Ready Egyptian diets: the coach's calculator and picker, the member's plan and today's meals.
/// Mirrors `DailyTargetDto`, `ReadyDietSummaryDto`, `MealPlanDetailDto`, `TodayMealsDto` and
/// `ClientDietDto` on the backend (Nutrition module).
library;

import '../progress_journey/progress_journey_model.dart';

double _d(dynamic v) => (v as num?)?.toDouble() ?? 0;

int _i(dynamic v) => (v as num?)?.round() ?? 0;

Map<String, dynamic> _map(dynamic v) => v is Map ? Map<String, dynamic>.from(v) : <String, dynamic>{};

List<T> _list<T>(dynamic v, T Function(Map<String, dynamic>) parse) =>
    v is List ? [for (final item in v) parse(_map(item))] : <T>[];

enum DietSex {
  male('Male'),
  female('Female');

  final String api;
  const DietSex(this.api);

  /// From the profile's gender ("Male", "female", …); null when unknown.
  static DietSex? fromGender(String? gender) => switch (gender?.toLowerCase()) {
        'male' => male,
        'female' => female,
        _ => null,
      };
}

enum DietActivity {
  low('Low'),
  light('Light'),
  moderate('Moderate'),
  high('High');

  final String api;
  const DietActivity(this.api);
}

enum DietGoal {
  fatLoss('FatLoss'),
  maintain('Maintain'),
  muscleGain('MuscleGain');

  final String api;
  const DietGoal(this.api);

  static DietGoal? parse(String? value) {
    for (final g in values) {
      if (g.api.toLowerCase() == value?.toLowerCase()) return g;
    }
    return null;
  }

  /// The member's progress goal, when they set one.
  static DietGoal fromProgressGoal(ProgressGoalType? goal) => switch (goal) {
        ProgressGoalType.loseFat => fatLoss,
        ProgressGoalType.buildMuscle => muscleGain,
        _ => maintain,
      };
}

/// What the coach enters in the calculator (`BodyInput`).
class BodyInputModel {
  final DietSex sex;
  final int age;
  final double heightCm;
  final double weightKg;
  final DietActivity activity;
  final DietGoal goal;

  const BodyInputModel({
    required this.sex,
    required this.age,
    required this.heightCm,
    required this.weightKg,
    required this.activity,
    required this.goal,
  });

  Map<String, dynamic> toJson() => {
        'sex': sex.api,
        'age': age,
        'heightCm': heightCm,
        'weightKg': weightKg,
        'activity': activity.api,
        'goal': goal.api,
      };
}

/// The member's daily target: at rest, with activity, and for the goal with its macros.
class DailyTargetModel {
  final int bmr;
  final int maintenance;
  final int calories;
  final int proteinGrams;
  final int carbsGrams;
  final int fatGrams;
  final DietGoal goal;

  const DailyTargetModel({
    required this.bmr,
    required this.maintenance,
    required this.calories,
    required this.proteinGrams,
    required this.carbsGrams,
    required this.fatGrams,
    this.goal = DietGoal.maintain,
  });

  factory DailyTargetModel.fromJson(Map<String, dynamic> json) => DailyTargetModel(
        bmr: _i(json['bmr']),
        maintenance: _i(json['maintenance']),
        calories: _i(json['calories']),
        proteinGrams: _i(json['proteinGrams']),
        carbsGrams: _i(json['carbsGrams']),
        fatGrams: _i(json['fatGrams']),
        goal: DietGoal.parse(json['goal'] as String?) ?? DietGoal.maintain,
      );

  /// The body of `target` when giving a diet (`DietTargetInput`).
  Map<String, dynamic> toTargetJson() =>
      {'calories': calories, 'proteinGrams': proteinGrams, 'carbsGrams': carbsGrams, 'fatGrams': fatGrams};
}

/// A ready diet in the picker; [goal] null = fits any goal.
class ReadyDietSummaryModel {
  final String key;
  final String name;
  final String nameAr;
  final String description;
  final String descriptionAr;
  final DietGoal? goal;
  final int mealsPerDay;

  const ReadyDietSummaryModel({
    required this.key,
    required this.name,
    required this.nameAr,
    this.description = '',
    this.descriptionAr = '',
    this.goal,
    this.mealsPerDay = 5,
  });

  String displayName(bool isArabic) => isArabic && nameAr.isNotEmpty ? nameAr : name;

  String displayDescription(bool isArabic) => isArabic && descriptionAr.isNotEmpty ? descriptionAr : description;

  factory ReadyDietSummaryModel.fromJson(Map<String, dynamic> json) => ReadyDietSummaryModel(
        key: json['key'] as String? ?? '',
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String? ?? '',
        description: json['description'] as String? ?? '',
        descriptionAr: json['descriptionAr'] as String? ?? '',
        goal: DietGoal.parse(json['goal'] as String?),
        mealsPerDay: _i(json['mealsPerDay']),
      );
}

/// A food and its amount in a planned meal. The API names foods "عربي (English)".
class DietFoodModel {
  final String name;
  final double quantity;
  final String unit;
  final double calories;
  final double protein;
  final double carbs;
  final double fat;

  const DietFoodModel({
    required this.name,
    required this.quantity,
    required this.unit,
    this.calories = 0,
    this.protein = 0,
    this.carbs = 0,
    this.fat = 0,
  });

  /// "فول مدمس (Ful medames)" → the Arabic part, or the English one in brackets.
  String displayName(bool isArabic) {
    final open = name.indexOf(' (');
    if (open < 0 || !name.endsWith(')')) return name;
    return isArabic ? name.substring(0, open) : name.substring(open + 2, name.length - 1);
  }

  factory DietFoodModel.fromJson(Map<String, dynamic> json) => DietFoodModel(
        name: json['name'] as String? ?? '',
        quantity: _d(json['quantity']),
        unit: json['unit'] as String? ?? '',
        calories: _d(json['calories']),
        protein: _d(json['protein']),
        carbs: _d(json['carbs']),
        fat: _d(json['fat']),
      );
}

/// One planned meal (or its alternative): Arabic name, English description, foods and totals.
class DietMealModel {
  final String mealType;
  final String name;
  final String? description;
  final List<DietFoodModel> foods;
  final double calories;
  final double protein;
  final double carbs;
  final double fat;

  const DietMealModel({
    required this.mealType,
    required this.name,
    this.description,
    this.foods = const [],
    this.calories = 0,
    this.protein = 0,
    this.carbs = 0,
    this.fat = 0,
  });

  String displayName(bool isArabic) => !isArabic && (description?.isNotEmpty ?? false) ? description! : name;

  factory DietMealModel.fromJson(Map<String, dynamic> json) => DietMealModel(
        mealType: (json['mealType'] as String? ?? '').toLowerCase(),
        name: json['name'] as String? ?? '',
        description: json['description'] as String?,
        foods: _list(json['foodItems'], DietFoodModel.fromJson),
        calories: _d(json['totalCalories']),
        protein: _d(json['totalProtein']),
        carbs: _d(json['totalCarbs']),
        fat: _d(json['totalFat']),
      );
}

/// One day of a diet: its meals and the alternative for each meal (same position).
class DietDayModel {
  final String dayOfWeek;
  final List<DietMealModel> meals;
  final List<DietMealModel> alternatives;

  const DietDayModel({required this.dayOfWeek, this.meals = const [], this.alternatives = const []});

  double get calories => meals.fold(0, (sum, m) => sum + m.calories);

  DietMealModel? alternativeAt(int index) => index < alternatives.length ? alternatives[index] : null;

  /// 0 = Sunday … 6 = Saturday, as `DateTime.weekday % 7`.
  int get weekdayIndex => const ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
      .indexOf(dayOfWeek.toLowerCase());

  factory DietDayModel.fromJson(Map<String, dynamic> json) => DietDayModel(
        dayOfWeek: '${json['dayOfWeek'] ?? ''}',
        meals: _list(json['plannedMeals'], DietMealModel.fromJson),
        alternatives: _list(json['alternatives'], DietMealModel.fromJson),
      );
}

/// A member's diet plan (`MealPlanDetailDto`), days ordered Saturday → Friday.
class DietPlanModel {
  final String id;
  final String name;
  final String? description;
  final int dailyCalories;
  final int proteinGrams;
  final int carbsGrams;
  final int fatGrams;
  final List<DietDayModel> days;

  const DietPlanModel({
    required this.id,
    required this.name,
    this.description,
    this.dailyCalories = 0,
    this.proteinGrams = 0,
    this.carbsGrams = 0,
    this.fatGrams = 0,
    this.days = const [],
  });

  String displayName(bool isArabic) => !isArabic && (description?.isNotEmpty ?? false) ? description! : name;

  factory DietPlanModel.fromJson(Map<String, dynamic> json) {
    final macros = _map(json['macroTargets']);
    final days = _list(json['days'], DietDayModel.fromJson)
      ..sort((a, b) => ((a.weekdayIndex + 1) % 7).compareTo((b.weekdayIndex + 1) % 7));
    return DietPlanModel(
      id: '${json['id'] ?? ''}',
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      dailyCalories: _i(json['dailyCalorieTarget']),
      proteinGrams: _i(macros['proteinGrams']),
      carbsGrams: _i(macros['carbsGrams']),
      fatGrams: _i(macros['fatGrams']),
      days: days,
    );
  }
}

/// A ready diet scaled to the member's calories, before the coach gives it.
class ReadyDietPreviewModel {
  final ReadyDietSummaryModel summary;
  final int calories;
  final List<DietDayModel> days;

  const ReadyDietPreviewModel({required this.summary, required this.calories, this.days = const []});

  factory ReadyDietPreviewModel.fromJson(Map<String, dynamic> json) => ReadyDietPreviewModel(
        summary: ReadyDietSummaryModel.fromJson(_map(json['summary'])),
        calories: _i(json['calories']),
        days: _list(json['days'], DietDayModel.fromJson),
      );
}

/// One meal of today's plan, ticked or not.
class TodayMealModel {
  final int index;
  final DietMealModel meal;
  final DietMealModel? alternative;
  final bool done;

  const TodayMealModel({required this.index, required this.meal, this.alternative, this.done = false});

  factory TodayMealModel.fromJson(Map<String, dynamic> json) => TodayMealModel(
        index: _i(json['index']),
        meal: DietMealModel.fromJson(_map(json['meal'])),
        alternative: json['alternative'] == null ? null : DietMealModel.fromJson(_map(json['alternative'])),
        done: json['done'] as bool? ?? false,
      );
}

/// The member's home card: today's meals against the day's target.
class TodayMealsModel {
  final String planId;
  final String planName;
  final int targetCalories;
  final List<TodayMealModel> meals;
  final int eatenCalories;

  const TodayMealsModel({
    required this.planId,
    required this.planName,
    required this.targetCalories,
    this.meals = const [],
    this.eatenCalories = 0,
  });

  int get doneCount => meals.where((m) => m.done).length;

  bool get allDone => meals.isNotEmpty && doneCount == meals.length;

  factory TodayMealsModel.fromJson(Map<String, dynamic> json) => TodayMealsModel(
        planId: '${json['planId'] ?? ''}',
        planName: json['planName'] as String? ?? '',
        targetCalories: _i(json['targetCalories']),
        meals: _list(json['meals'], TodayMealModel.fromJson),
        eatenCalories: _i(json['eatenCalories']),
      );
}

/// One of the last 7 days for the coach: meals the member ticked out of the day's plan.
class DietDayAdherenceModel {
  final DateTime date;
  final int mealsDone;
  final int mealsPlanned;

  const DietDayAdherenceModel({required this.date, this.mealsDone = 0, this.mealsPlanned = 0});

  factory DietDayAdherenceModel.fromJson(Map<String, dynamic> json) => DietDayAdherenceModel(
        date: DateTime.tryParse('${json['date']}') ?? DateTime.now(),
        mealsDone: _i(json['mealsDone']),
        mealsPlanned: _i(json['mealsPlanned']),
      );
}

/// A client's current diet and their last week, for their coach.
class ClientDietModel {
  final DietPlanModel? plan;
  final List<DietDayAdherenceModel> lastWeek;

  const ClientDietModel({this.plan, this.lastWeek = const []});

  int get mealsDone => lastWeek.fold(0, (n, d) => n + d.mealsDone);

  int get mealsPlanned => lastWeek.fold(0, (n, d) => n + d.mealsPlanned);

  factory ClientDietModel.fromJson(Map<String, dynamic> json) => ClientDietModel(
        plan: json['plan'] == null ? null : DietPlanModel.fromJson(_map(json['plan'])),
        lastWeek: _list(json['lastWeek'], DietDayAdherenceModel.fromJson),
      );
}
