// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appName => 'جيم 360';

  @override
  String get login => 'تسجيل الدخول';

  @override
  String get register => 'تسجيل جديد';

  @override
  String get email => 'البريد الإلكتروني';

  @override
  String get password => 'كلمة المرور';

  @override
  String get confirmPassword => 'تأكيد كلمة المرور';

  @override
  String get forgotPassword => 'نسيت كلمة المرور؟';

  @override
  String get resetPassword => 'إعادة تعيين كلمة المرور';

  @override
  String get signInWithGoogle => 'تسجيل بجوجل';

  @override
  String get signInWithApple => 'تسجيل بأبل';

  @override
  String get fullName => 'الاسم الكامل';

  @override
  String get phoneNumber => 'رقم الهاتف';

  @override
  String get logout => 'تسجيل الخروج';

  @override
  String get welcomeBack => 'مرحبًا بعودتك';

  @override
  String get createAccount => 'إنشاء حساب';

  @override
  String get orContinueWith => 'أو المتابعة عبر';

  @override
  String get agreeToTerms => 'أوافق على الشروط والأحكام';

  @override
  String get alreadyHaveAccount => 'لديك حساب بالفعل؟';

  @override
  String get dontHaveAccount => 'ليس لديك حساب؟';

  @override
  String get verificationCodeSent => 'تم إرسال رمز التحقق';

  @override
  String get enterVerificationCode => 'أدخل رمز التحقق';

  @override
  String get resendCode => 'إعادة إرسال الرمز';

  @override
  String get home => 'الرئيسية';

  @override
  String get workouts => 'التمارين';

  @override
  String get progress => 'التقدم';

  @override
  String get schedule => 'الجدول';

  @override
  String get more => 'المزيد';

  @override
  String get clients => 'العملاء';

  @override
  String get programs => 'البرامج';

  @override
  String get dashboard => 'لوحة التحكم';

  @override
  String get chat => 'الدردشة';

  @override
  String get nutrition => 'التغذية';

  @override
  String get billing => 'الفواتير';

  @override
  String get goodMorning => 'صباح الخير';

  @override
  String get goodAfternoon => 'مساء الخير';

  @override
  String get goodEvening => 'مساء الخير';

  @override
  String greetingWithName(String name) {
    return 'أهلاً $name';
  }

  @override
  String get todaysWorkout => 'تمرين اليوم';

  @override
  String get nextSession => 'الجلسة القادمة';

  @override
  String get streakDays => 'أيام متتالية';

  @override
  String streakCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count يوم متتالي',
      few: '$count أيام متتالية',
      two: 'يومان متتاليان',
      one: 'يوم واحد متتالي',
      zero: 'لا أيام متتالية',
    );
    return '$_temp0';
  }

  @override
  String get quickActions => 'إجراءات سريعة';

  @override
  String get recentActivity => 'النشاط الأخير';

  @override
  String get startWorkout => 'ابدأ التمرين';

  @override
  String get viewAll => 'عرض الكل';

  @override
  String get noWorkoutToday => 'لا يوجد تمرين مجدول اليوم';

  @override
  String get upcomingClasses => 'الحصص القادمة';

  @override
  String get exerciseLibrary => 'مكتبة التمارين';

  @override
  String get buildWorkout => 'بناء تمرين';

  @override
  String get workoutTemplates => 'قوالب التمارين';

  @override
  String get sets => 'مجموعات';

  @override
  String get reps => 'تكرارات';

  @override
  String get weight => 'وزن';

  @override
  String get rest => 'راحة';

  @override
  String get duration => 'المدة';

  @override
  String get distance => 'المسافة';

  @override
  String get exerciseName => 'اسم التمرين';

  @override
  String get muscleGroup => 'المجموعة العضلية';

  @override
  String get chest => 'صدر';

  @override
  String get back => 'رجوع';

  @override
  String get shoulders => 'أكتاف';

  @override
  String get biceps => 'باي';

  @override
  String get triceps => 'تراي';

  @override
  String get legs => 'أرجل';

  @override
  String get abs => 'بطن';

  @override
  String get cardio => 'كارديو';

  @override
  String get fullBody => 'جسم كامل';

  @override
  String get workoutName => 'اسم التمرين';

  @override
  String get addExercise => 'إضافة تمرين';

  @override
  String get removeExercise => 'حذف التمرين';

  @override
  String get reorderExercises => 'ترتيب التمارين';

  @override
  String get superSet => 'سوبر سِت';

  @override
  String get dropSet => 'دروب سِت';

  @override
  String get warmUp => 'إحماء';

  @override
  String get coolDown => 'تهدئة';

  @override
  String get personalRecord => 'رقم شخصي جديد!';

  @override
  String get workoutComplete => 'تم إنهاء التمرين!';

  @override
  String get totalVolume => 'الحجم الكلي';

  @override
  String get workoutDuration => 'مدة التمرين';

  @override
  String get assignWorkout => 'تعيين تمرين';

  @override
  String get assignProgram => 'تعيين برنامج';

  @override
  String get programName => 'اسم البرنامج';

  @override
  String get programWeeks => 'عدد الأسابيع';

  @override
  String get daysPerWeek => 'أيام في الأسبوع';

  @override
  String get remoteWorkout => 'تمرين عن بُعد';

  @override
  String get remoteWorkoutEnabled => 'التمرين عن بُعد مُفعّل';

  @override
  String get remoteWorkoutDisabled => 'التمرين عن بُعد مُعطّل';

  @override
  String get remoteReason => 'سبب التفعيل';

  @override
  String get workoutHistory => 'سجل التمارين';

  @override
  String get noWorkoutsYet => 'لا توجد تمارين بعد';

  @override
  String get bodyWeight => 'الوزن';

  @override
  String get bodyFat => 'نسبة الدهون';

  @override
  String get measurements => 'القياسات';

  @override
  String get progressPhotos => 'صور التقدم';

  @override
  String get compare => 'مقارنة';

  @override
  String get addMeasurement => 'إضافة قياس';

  @override
  String get waist => 'الخصر';

  @override
  String get hips => 'الأرداف';

  @override
  String get chestMeasurement => 'الصدر';

  @override
  String get armLeft => 'الذراع الأيسر';

  @override
  String get armRight => 'الذراع الأيمن';

  @override
  String get thighLeft => 'الفخذ الأيسر';

  @override
  String get thighRight => 'الفخذ الأيمن';

  @override
  String get takePhoto => 'التقاط صورة';

  @override
  String get chooseFromGallery => 'اختيار من المعرض';

  @override
  String get front => 'أمامي';

  @override
  String get side => 'جانبي';

  @override
  String get backPose => 'خلفي';

  @override
  String get progressChart => 'مخطط التقدم';

  @override
  String get noProgressData => 'لا توجد بيانات تقدم بعد';

  @override
  String get goal => 'الهدف';

  @override
  String get current => 'الحالي';

  @override
  String get change => 'التغيير';

  @override
  String get bookSession => 'حجز جلسة';

  @override
  String get cancelSession => 'إلغاء الجلسة';

  @override
  String get cancelBooking => 'إلغاء الحجز';

  @override
  String get waitingList => 'قائمة الانتظار';

  @override
  String get groupClasses => 'الحصص الجماعية';

  @override
  String get availableSlots => 'الأماكن المتاحة';

  @override
  String get fullyBooked => 'مكتمل';

  @override
  String get joinWaitingList => 'الانضمام لقائمة الانتظار';

  @override
  String get sessionDetails => 'تفاصيل الجلسة';

  @override
  String get sessionDate => 'تاريخ الجلسة';

  @override
  String get sessionTime => 'وقت الجلسة';

  @override
  String get sessionType => 'نوع الجلسة';

  @override
  String get privateSession => 'جلسة خاصة';

  @override
  String get groupSession => 'جلسة جماعية';

  @override
  String get confirmed => 'مؤكد';

  @override
  String get pending => 'قيد الانتظار';

  @override
  String get cancelled => 'ملغي';

  @override
  String get completed => 'مكتمل';

  @override
  String get noShow => 'لم يحضر';

  @override
  String get lateCancel => 'إلغاء متأخر';

  @override
  String get lateCancelWarning => 'سيتم خصم جلسة من باقتك عند الإلغاء المتأخر';

  @override
  String cancelWindowHours(int hours) {
    return 'يجب الإلغاء قبل $hours ساعة على الأقل';
  }

  @override
  String get trainerAvailability => 'أوقات المدرب المتاحة';

  @override
  String get setAvailability => 'تعيين الأوقات المتاحة';

  @override
  String get noSessionsScheduled => 'لا توجد جلسات مجدولة';

  @override
  String get liveSessionMode => 'وضع الجلسة الحية';

  @override
  String get startSession => 'بدء الجلسة';

  @override
  String get endSession => 'إنهاء الجلسة';

  @override
  String get sessionNotes => 'ملاحظات الجلسة';

  @override
  String get trainerNotes => 'ملاحظات المدرب';

  @override
  String get foodLog => 'سجل الطعام';

  @override
  String get waterTracking => 'تتبع المياه';

  @override
  String get mealPlan => 'خطة الوجبات';

  @override
  String get addFood => 'إضافة طعام';

  @override
  String get searchFood => 'بحث عن طعام';

  @override
  String get calories => 'سعرات حرارية';

  @override
  String get protein => 'بروتين';

  @override
  String get carbs => 'كربوهيدرات';

  @override
  String get fat => 'دهون';

  @override
  String get fiber => 'ألياف';

  @override
  String get breakfast => 'فطور';

  @override
  String get lunch => 'غداء';

  @override
  String get dinner => 'عشاء';

  @override
  String get snack => 'وجبة خفيفة';

  @override
  String get servingSize => 'حجم الحصة';

  @override
  String get gram => 'جرام';

  @override
  String get cup => 'كوب';

  @override
  String get piece => 'قطعة';

  @override
  String get dailyGoal => 'الهدف اليومي';

  @override
  String waterCups(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count كوب',
      few: '$count أكواب',
      two: 'كوبان',
      one: 'كوب واحد',
      zero: 'لم تشرب ماء',
    );
    return '$_temp0';
  }

  @override
  String get addWater => 'إضافة ماء';

  @override
  String get remaining => 'المتبقي';

  @override
  String get consumed => 'المستهلك';

  @override
  String get macros => 'المغذيات الكبرى';

  @override
  String get noMealPlan => 'لا توجد خطة وجبات';

  @override
  String get conversations => 'المحادثات';

  @override
  String get typeMessage => 'اكتب رسالة...';

  @override
  String get sendMessage => 'إرسال';

  @override
  String get announcements => 'الإعلانات';

  @override
  String get notifications => 'الإشعارات';

  @override
  String get markAllRead => 'تحديد الكل كمقروء';

  @override
  String get noNotifications => 'لا توجد إشعارات';

  @override
  String get noConversations => 'لا توجد محادثات';

  @override
  String get newAnnouncement => 'إعلان جديد';

  @override
  String get announcementTitle => 'عنوان الإعلان';

  @override
  String get announcementBody => 'نص الإعلان';

  @override
  String get notificationPreferences => 'تفضيلات الإشعارات';

  @override
  String get pushNotifications => 'الإشعارات الفورية';

  @override
  String get emailNotifications => 'إشعارات البريد الإلكتروني';

  @override
  String get recordPayment => 'تسجيل دفعة';

  @override
  String get packages => 'الباقات';

  @override
  String get receipt => 'إيصال';

  @override
  String get cash => 'نقدي';

  @override
  String get card => 'بطاقة';

  @override
  String get bankTransfer => 'تحويل بنكي';

  @override
  String get paymentAmount => 'المبلغ';

  @override
  String get paymentDate => 'تاريخ الدفع';

  @override
  String get paymentStatus => 'حالة الدفع';

  @override
  String get paid => 'مدفوع';

  @override
  String get voided => 'ملغي';

  @override
  String get overdue => 'متأخر';

  @override
  String get membershipPlan => 'خطة العضوية';

  @override
  String get renewMembership => 'تجديد العضوية';

  @override
  String get membershipExpiry => 'تاريخ انتهاء العضوية';

  @override
  String get membershipExpired => 'العضوية منتهية';

  @override
  String get membershipActive => 'العضوية نشطة';

  @override
  String get sessionPackage => 'باقة الجلسات';

  @override
  String get remainingSessions => 'الجلسات المتبقية';

  @override
  String sessionsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count جلسة متبقية',
      few: '$count جلسات متبقية',
      two: 'جلستان متبقيتان',
      one: 'جلسة واحدة متبقية',
      zero: 'لا جلسات متبقية',
    );
    return '$_temp0';
  }

  @override
  String get viewReceipt => 'عرض الإيصال';

  @override
  String get paymentHistory => 'سجل المدفوعات';

  @override
  String get noPayments => 'لا توجد مدفوعات';

  @override
  String get egp => 'ج.م';

  @override
  String get leaderboard => 'لوحة المتصدرين';

  @override
  String get trainerRating => 'تقييم المدرب';

  @override
  String get trainerOfTheMonth => 'مدرب الشهر';

  @override
  String get certificates => 'شهادات';

  @override
  String get badges => 'الأوسمة';

  @override
  String get points => 'النقاط';

  @override
  String get rank => 'الترتيب';

  @override
  String get rateTrainer => 'قيّم المدرب';

  @override
  String get ratingSubmitted => 'تم إرسال التقييم';

  @override
  String get optOutLeaderboard => 'إلغاء الاشتراك في لوحة المتصدرين';

  @override
  String get optInLeaderboard => 'الاشتراك في لوحة المتصدرين';

  @override
  String get trainerProfile => 'ملف المدرب';

  @override
  String get monthlyScore => 'النتيجة الشهرية';

  @override
  String get noLeaderboardData => 'لا توجد بيانات في لوحة المتصدرين';

  @override
  String get checkIn => 'تسجيل الحضور';

  @override
  String get memberNumber => 'رقم العضو';

  @override
  String get scanQR => 'مسح QR';

  @override
  String welcomeTo(String gymName) {
    return 'مرحبًا بك في $gymName';
  }

  @override
  String get membershipExpiredCheckIn => 'عضويتك منتهية';

  @override
  String get checkInSuccess => 'تم تسجيل الحضور بنجاح';

  @override
  String get checkInFailed => 'فشل تسجيل الحضور';

  @override
  String get kioskMode => 'وضع الكيوسك';

  @override
  String get enterMemberNumber => 'أدخل رقم العضو';

  @override
  String get numpadEntry => 'إدخال رقمي';

  @override
  String get qrEntry => 'مسح QR';

  @override
  String get staff => 'الموظفين';

  @override
  String get owner => 'المالك';

  @override
  String get manager => 'المدير';

  @override
  String get receptionist => 'موظف الاستقبال';

  @override
  String get trainer => 'مدرب';

  @override
  String get member => 'عضو';

  @override
  String get admin => 'مسؤول';

  @override
  String get addStaff => 'إضافة موظف';

  @override
  String get editStaff => 'تعديل موظف';

  @override
  String get removeStaff => 'حذف موظف';

  @override
  String get staffRole => 'دور الموظف';

  @override
  String get settings => 'الإعدادات';

  @override
  String get language => 'اللغة';

  @override
  String get arabic => 'العربية';

  @override
  String get english => 'English';

  @override
  String get units => 'الوحدات';

  @override
  String get kg => 'كيلوجرام';

  @override
  String get lb => 'رطل';

  @override
  String get cm => 'سنتيمتر';

  @override
  String get inch => 'إنش';

  @override
  String get downloadMyData => 'تحميل بياناتي';

  @override
  String get deleteMyAccount => 'حذف حسابي';

  @override
  String get darkMode => 'الوضع الداكن';

  @override
  String get lightMode => 'الوضع الفاتح';

  @override
  String get systemTheme => 'حسب النظام';

  @override
  String get appVersion => 'إصدار التطبيق';

  @override
  String get termsOfService => 'شروط الخدمة';

  @override
  String get privacyPolicy => 'سياسة الخصوصية';

  @override
  String get aboutUs => 'عن التطبيق';

  @override
  String get contactSupport => 'التواصل مع الدعم';

  @override
  String get profile => 'الملف الشخصي';

  @override
  String get editProfile => 'تعديل الملف الشخصي';

  @override
  String get changePassword => 'تغيير كلمة المرور';

  @override
  String get currentPassword => 'كلمة المرور الحالية';

  @override
  String get newPassword => 'كلمة المرور الجديدة';

  @override
  String get freePlan => 'الخطة المجانية';

  @override
  String get starterPlan => 'المبتدئ';

  @override
  String get proPlan => 'الاحترافي';

  @override
  String get businessPlan => 'الأعمال';

  @override
  String get upgrade => 'ترقية';

  @override
  String get featureNotAvailable => 'ميزة غير متاحة في خطتك الحالية';

  @override
  String get upgradeToUnlock => 'قم بالترقية لفتح هذه الميزة';

  @override
  String get currentPlan => 'الخطة الحالية';

  @override
  String get planFeatures => 'مميزات الخطة';

  @override
  String get comparePlans => 'مقارنة الخطط';

  @override
  String get storageLimitReached => 'تم الوصول لحد التخزين';

  @override
  String storageUsed(String used, String total) {
    return 'التخزين المستخدم: $used من $total';
  }

  @override
  String get save => 'حفظ';

  @override
  String get cancel => 'إلغاء';

  @override
  String get confirm => 'تأكيد';

  @override
  String get delete => 'حذف';

  @override
  String get edit => 'تعديل';

  @override
  String get add => 'إضافة';

  @override
  String get search => 'بحث';

  @override
  String get filter => 'تصفية';

  @override
  String get sort => 'ترتيب';

  @override
  String get loading => 'تحميل...';

  @override
  String get error => 'خطأ';

  @override
  String get empty => 'فارغ';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get ok => 'حسنًا';

  @override
  String get yes => 'نعم';

  @override
  String get no => 'لا';

  @override
  String get close => 'إغلاق';

  @override
  String get done => 'تم';

  @override
  String get next => 'التالي';

  @override
  String get previous => 'السابق';

  @override
  String get submit => 'إرسال';

  @override
  String get refresh => 'تحديث';

  @override
  String get share => 'مشاركة';

  @override
  String get copy => 'نسخ';

  @override
  String get selectAll => 'تحديد الكل';

  @override
  String get deselectAll => 'إلغاء التحديد';

  @override
  String get required => 'مطلوب';

  @override
  String get optional => 'اختياري';

  @override
  String get noResults => 'لا توجد نتائج';

  @override
  String get pullToRefresh => 'اسحب للتحديث';

  @override
  String get seeMore => 'عرض المزيد';

  @override
  String get seeLess => 'عرض أقل';

  @override
  String get comingSoon => 'قريبًا';

  @override
  String get dataProcessingConsent => 'الموافقة على معالجة البيانات';

  @override
  String get dataProcessingConsentDescription =>
      'أوافق على معالجة بياناتي الشخصية وفقًا لقانون حماية البيانات الشخصية المصري';

  @override
  String get marketingConsent => 'الموافقة على التسويق';

  @override
  String get marketingConsentDescription =>
      'أوافق على تلقي رسائل تسويقية وعروض خاصة';

  @override
  String get downloadMyDataDescription =>
      'تحميل نسخة من جميع بياناتك الشخصية المخزنة';

  @override
  String get deleteMyAccountDescription =>
      'حذف حسابك وجميع بياناتك نهائيًا. لا يمكن التراجع عن هذا الإجراء.';

  @override
  String get areYouSure => 'هل أنت متأكد؟';

  @override
  String get deleteAccountConfirmation =>
      'سيتم حذف حسابك وجميع بياناتك بشكل نهائي. هل تريد المتابعة؟';

  @override
  String get dataExportRequested =>
      'تم طلب تصدير البيانات. ستتلقى إشعارًا عند جهوز الملف.';

  @override
  String get consentUpdated => 'تم تحديث الموافقة';

  @override
  String get privacySettings => 'إعدادات الخصوصية';

  @override
  String dataRetentionNotice(int days) {
    return 'سيتم الاحتفاظ ببياناتك لمدة $days يومًا بعد حذف الحساب وفقًا للقانون';
  }

  @override
  String get gym => 'الجيم';

  @override
  String get members => 'الأعضاء';

  @override
  String membersCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count عضو',
      few: '$count أعضاء',
      two: 'عضوان',
      one: 'عضو واحد',
      zero: 'لا أعضاء',
    );
    return '$_temp0';
  }

  @override
  String get trainers => 'المدربين';

  @override
  String trainersCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count مدرب',
      few: '$count مدربين',
      two: 'مدربان',
      one: 'مدرب واحد',
      zero: 'لا مدربين',
    );
    return '$_temp0';
  }

  @override
  String get classes => 'الحصص';

  @override
  String classesCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count حصة',
      few: '$count حصص',
      two: 'حصتان',
      one: 'حصة واحدة',
      zero: 'لا حصص',
    );
    return '$_temp0';
  }

  @override
  String get exercises => 'التمارين';

  @override
  String get invoices => 'الفواتير';

  @override
  String get addMember => 'إضافة عضو';

  @override
  String get editMember => 'تعديل عضو';

  @override
  String get memberDetails => 'تفاصيل العضو';

  @override
  String memberSince(String date) {
    return 'عضو منذ $date';
  }

  @override
  String get addTrainer => 'إضافة مدرب';

  @override
  String get addClass => 'إضافة حصة';

  @override
  String get classCapacity => 'سعة الحصة';

  @override
  String get className => 'اسم الحصة';

  @override
  String get classSchedule => 'جدول الحصص';

  @override
  String get gymInfo => 'معلومات الجيم';

  @override
  String get gymName => 'اسم الجيم';

  @override
  String get gymAddress => 'عنوان الجيم';

  @override
  String get operatingHours => 'ساعات العمل';

  @override
  String get seasonalHours => 'ساعات العمل الموسمية';

  @override
  String get gymSections => 'أقسام الجيم';

  @override
  String get mensSection => 'قسم الرجال';

  @override
  String get womensSection => 'قسم النساء';

  @override
  String get mixedSection => 'قسم مختلط';

  @override
  String get switchGym => 'تبديل الجيم';

  @override
  String get selectGym => 'اختيار الجيم';

  @override
  String get analytics => 'التحليلات';

  @override
  String get reports => 'التقارير';

  @override
  String get gymSuspended => 'تم تعليق اشتراك الجيم';

  @override
  String get gymSuspendedMessage =>
      'يرجى التواصل مع إدارة الجيم للمزيد من المعلومات';

  @override
  String get contactGymAdmin => 'التواصل مع إدارة الجيم';

  @override
  String get accountSuspended => 'الحساب معلّق';

  @override
  String get subscriptionExpired => 'الاشتراك منتهي';

  @override
  String get somethingWentWrong => 'حدث خطأ ما';

  @override
  String get memberNotFound => 'لم يتم العثور على العضو';

  @override
  String get sessionNotAvailable => 'الجلسة غير متاحة';

  @override
  String get noInternetConnection => 'لا يوجد اتصال بالإنترنت';

  @override
  String get serverError => 'خطأ في الخادم';

  @override
  String get unauthorizedError => 'غير مصرح لك بهذا الإجراء';

  @override
  String get sessionExpired =>
      'انتهت صلاحية الجلسة. يرجى تسجيل الدخول مرة أخرى.';

  @override
  String get invalidInput => 'بيانات غير صحيحة';

  @override
  String get emailAlreadyExists => 'البريد الإلكتروني مسجل بالفعل';

  @override
  String get weakPassword => 'كلمة المرور ضعيفة';

  @override
  String get passwordMismatch => 'كلمات المرور غير متطابقة';

  @override
  String get fieldRequired => 'هذا الحقل مطلوب';

  @override
  String get invalidEmail => 'بريد إلكتروني غير صحيح';

  @override
  String get invalidPhone => 'رقم هاتف غير صحيح';

  @override
  String get fileTooLarge => 'حجم الملف كبير جدًا';

  @override
  String get uploadFailed => 'فشل رفع الملف';

  @override
  String get timeoutError => 'انتهت مهلة الطلب';

  @override
  String get trainerNotFound => 'لم يتم العثور على المدرب';

  @override
  String get classFullError => 'الحصة مكتملة';

  @override
  String get alreadyBooked => 'لديك حجز بالفعل';

  @override
  String get cannotCancelPastSession => 'لا يمكن إلغاء جلسة سابقة';

  @override
  String get today => 'اليوم';

  @override
  String get yesterday => 'أمس';

  @override
  String get tomorrow => 'غدًا';

  @override
  String get thisWeek => 'هذا الأسبوع';

  @override
  String get thisMonth => 'هذا الشهر';

  @override
  String get lastWeek => 'الأسبوع الماضي';

  @override
  String get lastMonth => 'الشهر الماضي';

  @override
  String minutesAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'منذ $count دقيقة',
      few: 'منذ $count دقائق',
      two: 'منذ دقيقتين',
      one: 'منذ دقيقة',
    );
    return '$_temp0';
  }

  @override
  String hoursAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'منذ $count ساعة',
      few: 'منذ $count ساعات',
      two: 'منذ ساعتين',
      one: 'منذ ساعة',
    );
    return '$_temp0';
  }

  @override
  String daysAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'منذ $count يوم',
      few: 'منذ $count أيام',
      two: 'منذ يومين',
      one: 'منذ يوم',
    );
    return '$_temp0';
  }

  @override
  String get justNow => 'الآن';

  @override
  String get date => 'التاريخ';

  @override
  String get time => 'الوقت';

  @override
  String get from => 'من';

  @override
  String get to => 'إلى';

  @override
  String get am => 'صباحًا';

  @override
  String get pm => 'مساءً';

  @override
  String get saturday => 'السبت';

  @override
  String get sunday => 'الأحد';

  @override
  String get monday => 'الاثنين';

  @override
  String get tuesday => 'الثلاثاء';

  @override
  String get wednesday => 'الأربعاء';

  @override
  String get thursday => 'الخميس';

  @override
  String get friday => 'الجمعة';

  @override
  String get membersLabel => 'عضو';

  @override
  String get sessionsLabel => 'جلسة';

  @override
  String workoutsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count تمرين',
      few: '$count تمارين',
      two: 'تمرينان',
      one: 'تمرين واحد',
      zero: 'لا تمارين',
    );
    return '$_temp0';
  }

  @override
  String daysCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count يوم',
      few: '$count أيام',
      two: 'يومان',
      one: 'يوم واحد',
      zero: 'لا أيام',
    );
    return '$_temp0';
  }

  @override
  String setsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count مجموعة',
      few: '$count مجموعات',
      two: 'مجموعتان',
      one: 'مجموعة واحدة',
      zero: 'لا مجموعات',
    );
    return '$_temp0';
  }

  @override
  String repsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count تكرار',
      few: '$count تكرارات',
      two: 'تكراران',
      one: 'تكرار واحد',
      zero: 'لا تكرارات',
    );
    return '$_temp0';
  }

  @override
  String notificationsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count إشعار',
      few: '$count إشعارات',
      two: 'إشعاران',
      one: 'إشعار واحد',
      zero: 'لا إشعارات',
    );
    return '$_temp0';
  }

  @override
  String paymentsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count دفعة',
      few: '$count مدفوعات',
      two: 'دفعتان',
      one: 'دفعة واحدة',
      zero: 'لا مدفوعات',
    );
    return '$_temp0';
  }

  @override
  String photosCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count صورة',
      few: '$count صور',
      two: 'صورتان',
      one: 'صورة واحدة',
      zero: 'لا صور',
    );
    return '$_temp0';
  }

  @override
  String get earnings => 'الأرباح';

  @override
  String get totalEarnings => 'إجمالي الأرباح';

  @override
  String get thisMonthEarnings => 'أرباح هذا الشهر';

  @override
  String get earningsReadOnly => 'الأرباح للعرض فقط';

  @override
  String get clientProfile => 'ملف العميل';

  @override
  String get clientWorkouts => 'تمارين العميل';

  @override
  String get clientProgress => 'تقدم العميل';

  @override
  String get clientNutrition => 'تغذية العميل';

  @override
  String get clientSchedule => 'جدول العميل';

  @override
  String get clientNotes => 'ملاحظات العميل';

  @override
  String get totalClients => 'إجمالي العملاء';

  @override
  String get activeClients => 'العملاء النشطين';

  @override
  String get sessionsToday => 'جلسات اليوم';

  @override
  String get sessionsThisWeek => 'جلسات هذا الأسبوع';

  @override
  String get active => 'نشط';

  @override
  String get inactive => 'غير نشط';

  @override
  String get suspended => 'معلّق';

  @override
  String get expired => 'منتهي';

  @override
  String get all => 'الكل';

  @override
  String get male => 'ذكر';

  @override
  String get female => 'أنثى';

  @override
  String get age => 'العمر';

  @override
  String get height => 'الطول';

  @override
  String get dateOfBirth => 'تاريخ الميلاد';

  @override
  String get gender => 'الجنس';

  @override
  String get status => 'الحالة';

  @override
  String get notes => 'ملاحظات';

  @override
  String get description => 'الوصف';

  @override
  String get title => 'العنوان';

  @override
  String get type => 'النوع';

  @override
  String get category => 'الفئة';

  @override
  String get total => 'الإجمالي';

  @override
  String get average => 'المتوسط';

  @override
  String get min => 'الأدنى';

  @override
  String get max => 'الأعلى';

  @override
  String get percentage => 'النسبة';

  @override
  String get offlineMode => 'وضع عدم الاتصال';

  @override
  String get offlineChangesWillSync =>
      'سيتم مزامنة التغييرات عند الاتصال بالإنترنت';

  @override
  String get syncing => 'جارٍ المزامنة...';

  @override
  String get syncComplete => 'تمت المزامنة';

  @override
  String get syncFailed => 'فشلت المزامنة';

  @override
  String lastSynced(String time) {
    return 'آخر مزامنة: $time';
  }

  @override
  String get configUpdated => 'تم تحديث الإعدادات';

  @override
  String get newFeatureAvailable => 'ميزة جديدة متاحة!';

  @override
  String get appUpdateAvailable => 'تحديث جديد متاح للتطبيق';

  @override
  String get updateNow => 'تحديث الآن';

  @override
  String get later => 'لاحقًا';

  @override
  String get successMessage => 'تمت العملية بنجاح';

  @override
  String get savedSuccessfully => 'تم الحفظ بنجاح';

  @override
  String get deletedSuccessfully => 'تم الحذف بنجاح';

  @override
  String get copied => 'تم النسخ';

  @override
  String get sessionPolicies => 'سياسات الجلسات';

  @override
  String get cancellationWindow => 'نافذة الإلغاء';

  @override
  String get noShowPolicy => 'سياسة عدم الحضور';

  @override
  String get lateCancelPolicy => 'سياسة الإلغاء المتأخر';

  @override
  String get deductFromPackage => 'خصم من الباقة';

  @override
  String get exportAsPdf => 'تصدير كـ PDF';

  @override
  String get print => 'طباعة';

  @override
  String get downloadReport => 'تحميل التقرير';

  @override
  String get dateRange => 'نطاق التاريخ';

  @override
  String get startDate => 'تاريخ البدء';

  @override
  String get endDate => 'تاريخ الانتهاء';

  @override
  String get apply => 'تطبيق';

  @override
  String get reset => 'إعادة تعيين';

  @override
  String get noData => 'لا توجد بيانات';

  @override
  String get totalMembers => 'إجمالي الأعضاء';

  @override
  String get newMembers => 'أعضاء جدد';

  @override
  String get activeMembers => 'الأعضاء النشطين';

  @override
  String get revenue => 'الإيرادات';

  @override
  String get attendance => 'الحضور';

  @override
  String get attendanceRate => 'نسبة الحضور';

  @override
  String get checkInCount => 'عدد مرات الحضور';

  @override
  String get popularClasses => 'الحصص الأكثر شعبية';

  @override
  String get peakHours => 'ساعات الذروة';
}
