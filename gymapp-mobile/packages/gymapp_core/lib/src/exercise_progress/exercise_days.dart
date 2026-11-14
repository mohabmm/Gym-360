/// pt.exercise_progress: the coach's quick day labels. Stored in English so a day groups the
/// same whatever the coach's language; shown in the reader's language.
abstract final class ExerciseDays {
  static const presets = <String, String>{
    'Push Day': 'يوم الدفع',
    'Pull Day': 'يوم السحب',
    'Leg Day': 'يوم الأرجل',
    'Upper Body': 'الجزء العلوي',
    'Lower Body': 'الجزء السفلي',
    'Full Body': 'الجسم كامل',
  };

  /// Arabic for a preset; custom labels are shown as the coach wrote them.
  static String display(String label, bool isArabic) => isArabic ? presets[label] ?? label : label;
}
