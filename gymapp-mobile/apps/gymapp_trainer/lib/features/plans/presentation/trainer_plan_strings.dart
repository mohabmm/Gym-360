import 'package:gymapp_core/gymapp_core.dart';

/// Arabic / English copy for the coach's plan screens.
class TrainerPlanStrings extends PlanStrings {
  const TrainerPlanStrings(super.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get plan => _t('الخطة', 'Plan');
  String get templates => _t('قوالب الخطط', 'Plan templates');
  String get templatesSubtitle => _t('قوالب النادي وقوالبك', 'Gym templates and yours');
  String get gymTemplates => _t('قوالب النادي', 'Gym templates');
  String get myTemplates => _t('قوالبي', 'My templates');
  String get sample => _t('نموذج', 'Sample');
  String get givePlan => _t('اعطِ خطة', 'Give a plan');
  String get newPlan => _t('خطة جديدة', 'New plan');
  String get startBlank => _t('ابدأ من الصفر', 'Start blank');
  String get startBlankHint => _t('ابنِ خطة خاصة لهذا العميل', 'Build a plan just for this client');
  String get fromTemplate => _t('ابدأ من قالب', 'Start from a template');
  String get assignAsIs => _t('اعطِ كما هي', 'Give as is');
  String get customize => _t('عدّل قبل الإعطاء', 'Customize first');
  String get noPlan => _t('لا توجد خطة بعد', 'No plan yet');
  String get noPlanHint => _t('اعطِ العميل خطة يتابعها في الأيام اللي مش معاك فيها.',
      'Give your client a plan to follow on the days without you.');
  String get editPlan => _t('عدّل الخطة', 'Edit plan');
  String get replacePlan => _t('خطة جديدة بدلها', 'Replace with a new plan');
  String get saveAsTemplate => _t('احفظ كقالب', 'Save as template');
  String get history => _t('الخطط السابقة', 'Previous plans');
  String get planName => _t('اسم الخطة', 'Plan name');
  String get planNameAr => _t('الاسم بالعربي (اختياري)', 'Arabic name (optional)');
  String get goalLabel => _t('الهدف', 'Goal');
  String get goalTextHint => _t('اكتب الهدف', 'Describe the goal');
  String get durationLabel => _t('مدة الخطة', 'Duration');
  String get sameEveryWeek => _t('نفس الأيام كل أسبوع', 'Same days every week');
  String get copyWeekOne => _t('انسخ الأسبوع ١ لكل الأسابيع', 'Copy week 1 to all weeks');
  String get startDate => _t('تبدأ يوم', 'Starts on');
  String get addDay => _t('أضف يوم تمرين', 'Add a workout day');
  String get dayTitle => _t('اسم اليوم', 'Day name');
  String dayHint(int n) => _t('مثال: اليوم ${_letters[n % _letters.length]} – جزء علوي', 'e.g. Day ${'ABCDEFG'[n % 7]} – Upper body');
  String get addExercise => _t('أضف تمرين', 'Add exercise');
  String get duplicateDay => _t('كرّر اليوم', 'Duplicate day');
  String get deleteDay => _t('احذف اليوم', 'Delete day');
  String get assign => _t('اعطِ الخطة', 'Give plan');
  String get saveChanges => _t('احفظ التعديلات', 'Save changes');
  String get saveTemplate => _t('احفظ القالب', 'Save template');
  String get makeMyCopy => _t('اعمل نسخة لي', 'Make my copy');
  String get gymTemplateReadOnly => _t('قالب النادي يعدّله المدير فقط. اعمل نسخة لك وعدّلها.',
      'Only the manager edits gym templates. Make your own copy to change it.');
  String get assigned => _t('تم إعطاء الخطة ✅ سيصل إشعار للعميل', 'Plan given ✅ the client is notified');
  String get saved => _t('تم الحفظ', 'Saved');
  String get conflict => _t('الخطة اتعدلت من جهاز تاني. افتحها من جديد.', 'The plan was changed elsewhere. Open it again.');
  String get notYourClient => _t('هذا العميل ليس لديه باقة معك', 'This client has no package with you');
  String get saveFailed => _t('تعذّر الحفظ، حاول مرة أخرى', "Couldn't save. Try again.");
  String get incomplete => _t('أكمل اسم الخطة وكل يوم وتمارينه', 'Fill in the plan name and every day with its exercises');
  String workouts(int n) => _t('$n تمرين', n == 1 ? '1 workout' : '$n workouts');
  String templateCopyName(String name) => _t('$name (نسختي)', '$name (my copy)');

  // Exercise sheet
  String get exercise => _t('التمرين', 'Exercise');
  String get searchLibrary => _t('ابحث في مكتبة التمارين', 'Search the exercise library');
  String useFreeText(String text) => _t('استخدم «$text» كتمرين حر', 'Use "$text" as a free-text exercise');
  String get fromLibrary => _t('من المكتبة', 'From the library');
  String get setsLabel => _t('المجموعات', 'Sets');
  String get repsLabel => _t('العدات (مثال 8-10)', 'Reps (e.g. 8-10)');
  String get timeLabel => _t('الوقت (ثانية)', 'Time (seconds)');
  String get restLabel => _t('الراحة', 'Rest');
  String get targetWeight => _t('الوزن المستهدف (اختياري)', 'Target weight (optional)');
  String get rpeLabel => _t('RPE (اختياري)', 'RPE (optional)');
  String get notesLabel => _t('ملاحظات للمتدرب', 'Notes for the trainee');
  String get videoLabel => _t('رابط فيديو الأداء (اختياري)', 'How-to video link (optional)');
  String get done => _t('تم', 'Done');
  String get remove => _t('احذف', 'Remove');

  static const _letters = ['أ', 'ب', 'ج', 'د', 'هـ', 'و', 'ز'];
}
