import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_draft.dart';

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
  String get readyPrograms => _t('برامج جاهزة', 'Ready programs');
  String get readyProgramsHint => _t('برامج معروفة عالمياً وفي جيمات مصر – عدّل فيها براحتك',
      'Programs known worldwide and in Egyptian gyms – change anything you like');
  String get allCategories => _t('الكل', 'All');
  String category(ProgramCategory c) => switch (c) {
        ProgramCategory.classic => _t('كلاسيك عالمي', 'Classics'),
        ProgramCategory.egyptian => _t('جيمات مصر', 'Egyptian gyms'),
        ProgramCategory.lifestyle => _t('رمضان والبيت', 'Ramadan & home'),
        ProgramCategory.cardio => _t('كارديو', 'Cardio'),
      };
  String level(bool isBeginner) => isBeginner ? _t('مبتدئ', 'Beginner') : _t('متوسط', 'Intermediate');
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

  // pt.starter_plan
  String get starterTitle => _t('خطة بداية ٣ أيام', '3-day starter plan');
  String get starterHint => _t('ابدأ مع العميل بخطة ٣ أيام. لو غاب، الاستقبال يفتح لك برنامج أسبوع من القوالب.',
      'Start your client on a 3-day plan. If they stop coming, the desk unlocks a one-week program from the templates.');
  String get starterBlank => _t('ابنِ خطة ٣ أيام', 'Build a 3-day plan');
  String get starterFromProgramHint => _t('أو ابدأ من برنامج جاهز – هناخد أول ٣ أيام منه',
      'Or start from a ready program – its first 3 days are used');
  String get programsLocked => _t('القوالب مقفولة 🔒', 'Templates are locked 🔒');
  String get programsLockedHint => _t('لو العضو غاب، الاستقبال يقدر يفتح لك برنامج أسبوع أو ٣٠ يوم من القوالب.',
      'If the member stops coming, the desk can unlock a one-week or 30-day program from the templates.');
  String get weekTitle => _t('برنامج أسبوع', 'One-week program');
  String get monthTitle => _t('برنامج ٣٠ يوم', '30-day program');

  String limitTitle(PlanLimit limit) => switch (limit) {
        PlanLimit.oneWeek => weekTitle,
        PlanLimit.thirtyDays => monthTitle,
        _ => starterTitle,
      };
  String limitHint(PlanLimit limit) => switch (limit) {
        PlanLimit.oneWeek => _t('الاستقبال فتح لك أسبوع عشان العضو يرجع الجيم. اختار قالب أو برنامج جاهز – هناخد أول أسبوع منه، وبعده ادّيله الأكل.',
            'The desk unlocked one week to bring the member back. Pick a template or ready program – its first week is used – then give a diet.'),
        PlanLimit.thirtyDays => _t('الاستقبال فتح لك ٣٠ يوم. اختار قالب أو برنامج جاهز – لحد ٤ أسابيع منه، وبعده ادّيله الأكل.',
            'The desk unlocked 30 days. Pick a template or ready program – up to 4 weeks of it – then give a diet.'),
        _ => starterHint,
      };
  String limitBlank(PlanLimit limit) => limit == PlanLimit.starter ? starterBlank : _t('ابنِ من الصفر', 'Build from scratch');
  String limitRules(PlanLimit limit) => switch (limit) {
        PlanLimit.oneWeek => _t('برنامج أسبوع: لحد ٧ أيام تمرين، وينتهي بعد ٧ أيام.', 'One-week program: up to 7 workout days, ends after 7 days.'),
        PlanLimit.thirtyDays => _t('برنامج ٣٠ يوم: لحد ٤ أسابيع، وينتهي بعد ٣٠ يوم.', '30-day program: up to 4 weeks, ends after 30 days.'),
        _ => _t('خطة البداية: أسبوع واحد و٣ أيام تمرين بالكتير، وتنتهي بعد ٣ أيام.',
            'Starter plan: one week, up to 3 workout days, ends after 3 days.'),
      };
  String limitEnds(PlanLimit limit, String date) => _t('${limitTitle(limit)} · آخر يوم $date', '${limitTitle(limit)} · last day $date');
  String limitEnded(PlanLimit limit) => limit.isUnlocked
      ? _t('خلص ${limitTitle(limit)}. لو العضو لسه غايب، اطلب من الاستقبال برنامج تاني.',
          'The ${limitTitle(limit).toLowerCase()} is over. If the member is still away, ask the desk for another.')
      : _t('خلصت خطة البداية. لو العضو غاب، اطلب من الاستقبال يفتح لك برنامج أسبوع أو ٣٠ يوم.',
          'The starter plan is over. If the member stops coming, ask the desk to unlock a one-week or 30-day program.');
  String get starterOnlyError => _t('القوالب لسه مقفولة: اعطِ خطة ٣ أيام بس.', 'Templates are still locked: give a 3-day plan only.');
  String get weekOnlyError => _t('الاستقبال فتح أسبوع واحد بس: اعطِ أسبوع لحد ٧ أيام تمرين.',
      'The desk unlocked one week only: give one week of up to 7 workout days.');
  String get monthOnlyError => _t('الاستقبال فتح ٣٠ يوم: اعطِ لحد ٤ أسابيع.', 'The desk unlocked 30 days: give up to 4 weeks.');

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
