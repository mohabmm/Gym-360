import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

ExerciseModel _exercise({
  String name = 'Barbell Bench Press',
  String? nameAr = 'ضغط الصدر بالبار',
  List<String> muscleGroups = const ['Chest'],
}) =>
    ExerciseModel.fromJson({
      'id': 'ex-1',
      'name': name,
      'nameAr': nameAr,
      'muscleGroups': muscleGroups,
      'equipment': 'Barbell',
      'category': 'Strength',
      'scope': 'Global',
      'isActive': true,
    });

void main() {
  group('ExerciseLabels', () {
    // Wire values the BE enums define — every one needs an Arabic label.
    const muscleGroups = [
      'Chest', 'Back', 'Shoulders', 'Biceps', 'Triceps',
      'Legs', 'Glutes', 'Core', 'Cardio', 'FullBody',
    ];
    const equipment = [
      'Barbell', 'Dumbbell', 'Machine', 'Cable', 'Bodyweight',
      'Kettlebell', 'Band', 'Other', 'None',
    ];
    const categories = ['Strength', 'Cardio', 'Flexibility', 'Plyometric'];
    final arabic = RegExp(r'[؀-ۿ]');

    test('every backend enum value has an Arabic label', () {
      for (final v in muscleGroups) {
        expect(ExerciseLabels.muscleGroup(v, isArabic: true), matches(arabic),
            reason: v);
      }
      for (final v in equipment) {
        expect(ExerciseLabels.equipment(v, isArabic: true), matches(arabic),
            reason: v);
      }
      for (final v in categories) {
        expect(ExerciseLabels.category(v, isArabic: true), matches(arabic),
            reason: v);
      }
    });

    test('English keeps the enum name, tidying the unreadable ones', () {
      expect(ExerciseLabels.muscleGroup('Chest', isArabic: false), 'Chest');
      expect(ExerciseLabels.muscleGroup('FullBody', isArabic: false),
          'Full Body');
      expect(ExerciseLabels.equipment('None', isArabic: false), 'No equipment');
    });

    test('lookup is case-insensitive and unknown values pass through', () {
      expect(ExerciseLabels.muscleGroup('chest', isArabic: true), 'الصدر');
      expect(ExerciseLabels.muscleGroup('Forearms', isArabic: true), 'Forearms');
      expect(ExerciseLabels.equipment('', isArabic: true), '');
    });

    test('muscleGroups joins with the locale separator', () {
      expect(
        ExerciseLabels.muscleGroups(['Chest', 'Triceps'], isArabic: true),
        'الصدر، الترايسبس',
      );
      expect(
        ExerciseLabels.muscleGroups(['Chest', 'Triceps'], isArabic: false),
        'Chest, Triceps',
      );
    });

    test('name follows the locale and falls back to English', () {
      expect(ExerciseLabels.name(_exercise(), isArabic: true), 'ضغط الصدر بالبار');
      expect(ExerciseLabels.name(_exercise(), isArabic: false),
          'Barbell Bench Press');
      expect(ExerciseLabels.name(_exercise(nameAr: null), isArabic: true),
          'Barbell Bench Press');
      expect(ExerciseLabels.name(_exercise(nameAr: '  '), isArabic: true),
          'Barbell Bench Press');
    });

    test('matches searches both languages and muscle groups', () {
      final ex = _exercise();
      expect(ExerciseLabels.matches(ex, 'bench'), isTrue,
          reason: 'English name must stay searchable once nameAr is set');
      expect(ExerciseLabels.matches(ex, 'BENCH '), isTrue);
      expect(ExerciseLabels.matches(ex, 'ضغط'), isTrue);
      expect(ExerciseLabels.matches(ex, 'chest'), isTrue);
      expect(ExerciseLabels.matches(ex, 'الصدر'), isTrue);
      expect(ExerciseLabels.matches(ex, ''), isTrue);
      expect(ExerciseLabels.matches(ex, 'squat'), isFalse);
    });
  });
}
