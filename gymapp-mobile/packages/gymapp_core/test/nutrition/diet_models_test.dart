import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

Map<String, dynamic> _meal(String type, String name, double kcal) => {
      'mealType': type,
      'name': name,
      'description': 'Breakfast: ful & eggs',
      'foodItems': [
        {'name': 'فول مدمس (Ful medames)', 'quantity': 150.0, 'unit': 'g', 'calories': 141.0},
        {'name': 'عيش بلدي (Baladi bread (loaf))', 'quantity': 0.5, 'unit': 'pc', 'calories': 105.0},
      ],
      'totalCalories': kcal,
    };

void main() {
  test('the member plan parses meals, alternatives and decimal targets, Saturday first', () {
    final plan = DietPlanModel.fromJson({
      'id': 'p1',
      'name': 'تخسيس بأكل مصري',
      'description': 'Fat loss',
      'dailyCalorieTarget': 2210.00,
      'macroTargets': {'proteinGrams': 160.00, 'carbsGrams': 260, 'fatGrams': 60},
      'days': [
        {'dayOfWeek': 'monday', 'plannedMeals': [_meal('Breakfast', 'الفطار: فول وبيض', 400)], 'alternatives': null},
        {'dayOfWeek': 'saturday', 'plannedMeals': [_meal('Breakfast', 'الفطار: فول وبيض', 400)], 'alternatives': [_meal('Breakfast', 'بديل', 380)]},
      ],
    });

    expect(plan.dailyCalories, 2210);
    expect(plan.proteinGrams, 160);
    expect(plan.days.first.dayOfWeek, 'saturday', reason: 'the Egyptian week starts on Saturday');
    expect(plan.days.first.alternativeAt(0)?.calories, 380);
    expect(plan.days.last.alternativeAt(0), isNull);
    expect(plan.displayName(false), 'Fat loss');
  });

  test('food names split into Arabic and English and portions read naturally', () {
    final ful = DietFoodModel.fromJson({'name': 'فول مدمس (Ful medames)', 'quantity': 150, 'unit': 'g'});
    final bread = DietFoodModel.fromJson({'name': 'عيش بلدي (Baladi bread (loaf))', 'quantity': 1.5, 'unit': 'pc'});

    expect(ful.displayName(true), 'فول مدمس');
    expect(ful.displayName(false), 'Ful medames');
    expect(bread.displayName(false), 'Baladi bread (loaf)');
    expect(const DietStrings(true).amount(ful), '150 جم');
    expect(const DietStrings(false).amount(bread), '× 1½');
    expect(const DietStrings(true).amount(DietFoodModel.fromJson({'name': 'x', 'quantity': 0.25, 'unit': 'pc'})), '× ¼');
  });

  test('today counts ticked meals; the calculator body serialises to the API names', () {
    final today = TodayMealsModel.fromJson({
      'planId': 'p1',
      'planName': 'خطة',
      'targetCalories': 1850.0,
      'eatenCalories': 400.0,
      'meals': [
        {'index': 0, 'meal': _meal('Breakfast', 'فطار', 400), 'alternative': null, 'done': true},
        {'index': 1, 'meal': _meal('Snack', 'سناك', 200), 'done': false},
      ],
    });

    expect(today.doneCount, 1);
    expect(today.allDone, isFalse);
    expect(
      const BodyInputModel(sex: DietSex.female, age: 28, heightCm: 162, weightKg: 70, activity: DietActivity.light, goal: DietGoal.fatLoss)
          .toJson(),
      {'sex': 'Female', 'age': 28, 'heightCm': 162.0, 'weightKg': 70.0, 'activity': 'Light', 'goal': 'FatLoss'},
    );
    expect(DietSex.fromGender('male'), DietSex.male);
    expect(DietGoal.fromProgressGoal(ProgressGoalType.buildMuscle), DietGoal.muscleGain);
  });
}
