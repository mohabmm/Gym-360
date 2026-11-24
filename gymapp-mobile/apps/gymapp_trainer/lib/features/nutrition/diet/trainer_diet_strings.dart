import 'package:gymapp_core/gymapp_core.dart';

/// Arabic / English copy for the coach's diet screens.
class TrainerDietStrings extends DietStrings {
  const TrainerDietStrings(super.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get diet => _t('التغذية', 'Diet');
  String get giveDiet => _t('اعطِ خطة أكل', 'Give a diet plan');
  String get newDiet => _t('خطة أكل جديدة بدلها', 'Replace with a new diet');
  String get noDiet => _t('لا توجد خطة أكل بعد', 'No diet plan yet');
  String get noDietHint => _t('احسب سعراته واختار نظام أكل مصري جاهز على قد احتياجه – بيكمّل خطة التمرين.',
      'Calculate their calories and pick a ready Egyptian diet sized for them – it completes the training plan.');
  String get calculator => _t('حساب السعرات', 'Calorie calculator');
  String get sexLabel => _t('النوع', 'Sex');
  String get ageLabel => _t('السن', 'Age');
  String get heightLabel => _t('الطول (سم)', 'Height (cm)');
  String get weightLabel => _t('الوزن (كجم)', 'Weight (kg)');
  String get activityLabel => _t('النشاط', 'Activity');
  String get goalLabel => _t('الهدف', 'Goal');
  String get prefilled => _t('من بروفايل العميل وآخر قياس – راجعها', "From the client's profile and last measurement – check them");
  String get calculate => _t('احسب', 'Calculate');
  String get fillAll => _t('اكمل السن والطول والوزن', 'Fill in age, height and weight');
  String howCalculated(DailyTargetModel t) => _t(
      'الحرق وقت الراحة ${t.bmr} · مع نشاطه ${t.maintenance} · للهدف ${t.calories} سعرة',
      'At rest ${t.bmr} · with activity ${t.maintenance} · for the goal ${t.calories} kcal');
  String get pickDiet => _t('اختار نظام أكل جاهز', 'Pick a ready diet');
  String get preview => _t('شكل يوم في الخطة', 'A day on the plan');
  String give(int kcal) => _t('اعطِ الخطة · $kcal سعرة', 'Give plan · $kcal kcal');
  String get given => _t('تم إعطاء خطة الأكل ✅ وصل إشعار للعميل', 'Diet given ✅ the client is notified');
  String get failed => _t('تعذّر الحفظ، حاول مرة أخرى', "Couldn't save. Try again.");
  String get notYourClient => _t('هذا العميل ليس لديه باقة معك', 'This client has no package with you');
  String lastWeek(ClientDietModel d) => _t('آخر ٧ أيام: ${d.mealsDone} من ${d.mealsPlanned} وجبة',
      'Last 7 days: ${d.mealsDone} of ${d.mealsPlanned} meals');
  String get talkAtSession => _t('اتكلم معاه عن الالتزام في الحصة الجاية', 'Talk about it at the next session');
  String get onboardingTitle => _t('كمّل أونبوردينج العميل', "Complete the client's onboarding");
  String get onboardingBody => _t('خطة التمرين اتبعتت. تحب تديله خطة أكل على قد احتياجه دلوقتي؟',
      'The training plan is sent. Give a diet sized for them now?');
  String get later => _t('بعدين', 'Later');
  String get dietGoalAny => _t('لأي هدف', 'Any goal');
}
