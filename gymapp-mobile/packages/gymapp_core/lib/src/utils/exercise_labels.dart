import '../models/exercise_model.dart';

/// Display labels for the exercise library's backend enums (muscle group,
/// equipment, category) and locale-aware exercise names.
///
/// The API sends and expects the English enum names (`Chest`, `Barbell`, …) —
/// those stay the wire values. These helpers only translate what is shown, so
/// the Arabic UI no longer mixes in English tags.
class ExerciseLabels {
  ExerciseLabels._();

  static const Map<String, String> _muscleGroupsAr = {
    'chest': 'الصدر',
    'back': 'الظهر',
    'shoulders': 'الأكتاف',
    'biceps': 'البايسبس',
    'triceps': 'الترايسبس',
    'legs': 'الأرجل',
    'glutes': 'المؤخرة',
    'core': 'البطن',
    'cardio': 'كارديو',
    'fullbody': 'الجسم كامل',
  };

  static const Map<String, String> _equipmentAr = {
    'barbell': 'بار',
    'dumbbell': 'دمبل',
    'machine': 'جهاز',
    'cable': 'كابل',
    'bodyweight': 'وزن الجسم',
    'kettlebell': 'كيتل بل',
    'band': 'مطاط',
    'other': 'أخرى',
    'none': 'بدون معدات',
  };

  static const Map<String, String> _categoriesAr = {
    'strength': 'قوة',
    'cardio': 'كارديو',
    'flexibility': 'مرونة',
    'plyometric': 'بلايومتركس',
  };

  // English display forms for enum names that are not already readable.
  static const Map<String, String> _englishOverrides = {
    'fullbody': 'Full Body',
    'none': 'No equipment',
  };

  static String muscleGroup(String value, {required bool isArabic}) =>
      _label(value, _muscleGroupsAr, isArabic);

  static String equipment(String value, {required bool isArabic}) =>
      _label(value, _equipmentAr, isArabic);

  static String category(String value, {required bool isArabic}) =>
      _label(value, _categoriesAr, isArabic);

  /// Comma-separated muscle groups, e.g. "الصدر، الترايسبس".
  static String muscleGroups(
    Iterable<String> values, {
    required bool isArabic,
  }) =>
      values
          .map((v) => muscleGroup(v, isArabic: isArabic))
          .join(isArabic ? '، ' : ', ');

  /// Exercise name for the current locale: Arabic when available in the
  /// Arabic UI, otherwise the English name.
  static String name(ExerciseModel exercise, {required bool isArabic}) {
    final arabic = exercise.nameAr;
    return isArabic && arabic != null && arabic.trim().isNotEmpty
        ? arabic
        : exercise.name;
  }

  /// Whether [exercise] matches a free-text [query] in either language (name,
  /// Arabic name, or a muscle-group label). A trainer typing "bench" must
  /// still find an exercise that is displayed by its Arabic name.
  static bool matches(ExerciseModel exercise, String query) {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) return true;
    if (exercise.name.toLowerCase().contains(q)) return true;
    if (exercise.nameAr?.toLowerCase().contains(q) ?? false) return true;
    for (final group in exercise.muscleGroups) {
      if (group.toLowerCase().contains(q)) return true;
      if (muscleGroup(group, isArabic: true).contains(q)) return true;
    }
    return false;
  }

  static String _label(String value, Map<String, String> arabic, bool isArabic) {
    final key = value.trim().toLowerCase();
    if (key.isEmpty) return value;
    if (isArabic) return arabic[key] ?? value;
    return _englishOverrides[key] ?? value;
  }
}
