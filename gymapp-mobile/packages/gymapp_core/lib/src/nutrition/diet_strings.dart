import 'diet_models.dart';

/// Arabic / English copy shared by the coach's diet flow and the member's diet screens.
class DietStrings {
  final bool isArabic;

  const DietStrings(this.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String kcal(num n) => _t('${n.round()} سعرة', '${n.round()} kcal');
  String grams(num n) => _t('${n.round()} جم', '${n.round()} g');
  String get protein => _t('بروتين', 'Protein');
  String get carbs => _t('كارب', 'Carbs');
  String get fat => _t('دهون', 'Fat');
  String get alternative => _t('بديل', 'Swap');
  String get dailyTarget => _t('هدفك اليومي', 'Daily target');

  String mealType(String type) => switch (type.toLowerCase()) {
        'breakfast' => _t('فطار', 'Breakfast'),
        'lunch' => _t('غدا', 'Lunch'),
        'dinner' => _t('عشا', 'Dinner'),
        _ => _t('سناك', 'Snack'),
      };

  /// "150 جم", "250 مل", "× ½" (pieces: eggs, loaves, fruit).
  String amount(DietFoodModel food) => switch (food.unit) {
        'g' => _t('${_n(food.quantity)} جم', '${_n(food.quantity)} g'),
        'ml' => _t('${_n(food.quantity)} مل', '${_n(food.quantity)} ml'),
        _ => '× ${_n(food.quantity)}',
      };

  String goal(DietGoal goal) => switch (goal) {
        DietGoal.fatLoss => _t('خسارة دهون', 'Fat loss'),
        DietGoal.maintain => _t('ثبات الوزن', 'Keep weight'),
        DietGoal.muscleGain => _t('زيادة عضل', 'Muscle gain'),
      };

  String activity(DietActivity a) => switch (a) {
        DietActivity.low => _t('قليل الحركة', 'Mostly sitting'),
        DietActivity.light => _t('تمرين ١–٣ أيام', 'Trains 1–3 days'),
        DietActivity.moderate => _t('تمرين ٣–٥ أيام', 'Trains 3–5 days'),
        DietActivity.high => _t('تمرين ٦–٧ أيام / شغل مجهد', 'Trains 6–7 days / physical job'),
      };

  String sex(DietSex s) => s == DietSex.male ? _t('ذكر', 'Male') : _t('أنثى', 'Female');

  String weekday(int index) => isArabic
      ? const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'][index.clamp(0, 6)]
      : const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][index.clamp(0, 6)];

  static String _n(double v) {
    const fractions = {'0.25': '¼', '0.50': '½', '0.75': '¾'};
    final whole = v.truncate();
    final part = fractions[(v - whole).toStringAsFixed(2)];
    if (part == null) return v == whole ? '$whole' : v.toStringAsFixed(1);
    return whole == 0 ? part : '$whole$part';
  }
}
