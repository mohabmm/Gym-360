import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
      : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
    delegate,
    GlobalMaterialLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
  ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en')
  ];

  /// Application name
  ///
  /// In ar, this message translates to:
  /// **'جيم 360'**
  String get appName;

  /// Login button and page title
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الدخول'**
  String get login;

  /// Register / sign up
  ///
  /// In ar, this message translates to:
  /// **'تسجيل جديد'**
  String get register;

  /// Email field label
  ///
  /// In ar, this message translates to:
  /// **'البريد الإلكتروني'**
  String get email;

  /// Password field label
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور'**
  String get password;

  /// Confirm password field label
  ///
  /// In ar, this message translates to:
  /// **'تأكيد كلمة المرور'**
  String get confirmPassword;

  /// Forgot password link
  ///
  /// In ar, this message translates to:
  /// **'نسيت كلمة المرور؟'**
  String get forgotPassword;

  /// Reset password page title
  ///
  /// In ar, this message translates to:
  /// **'إعادة تعيين كلمة المرور'**
  String get resetPassword;

  /// Google sign-in button
  ///
  /// In ar, this message translates to:
  /// **'تسجيل بجوجل'**
  String get signInWithGoogle;

  /// Apple sign-in button
  ///
  /// In ar, this message translates to:
  /// **'تسجيل بأبل'**
  String get signInWithApple;

  /// Full name field
  ///
  /// In ar, this message translates to:
  /// **'الاسم الكامل'**
  String get fullName;

  /// Phone number field
  ///
  /// In ar, this message translates to:
  /// **'رقم الهاتف'**
  String get phoneNumber;

  /// Logout button
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الخروج'**
  String get logout;

  /// Welcome back greeting
  ///
  /// In ar, this message translates to:
  /// **'مرحبًا بعودتك'**
  String get welcomeBack;

  /// Create account button
  ///
  /// In ar, this message translates to:
  /// **'إنشاء حساب'**
  String get createAccount;

  /// Divider text between login form and social buttons
  ///
  /// In ar, this message translates to:
  /// **'أو المتابعة عبر'**
  String get orContinueWith;

  /// Terms agreement checkbox
  ///
  /// In ar, this message translates to:
  /// **'أوافق على الشروط والأحكام'**
  String get agreeToTerms;

  /// Link to login page
  ///
  /// In ar, this message translates to:
  /// **'لديك حساب بالفعل؟'**
  String get alreadyHaveAccount;

  /// Link to register page
  ///
  /// In ar, this message translates to:
  /// **'ليس لديك حساب؟'**
  String get dontHaveAccount;

  /// Verification code sent message
  ///
  /// In ar, this message translates to:
  /// **'تم إرسال رمز التحقق'**
  String get verificationCodeSent;

  /// Enter OTP prompt
  ///
  /// In ar, this message translates to:
  /// **'أدخل رمز التحقق'**
  String get enterVerificationCode;

  /// Resend verification code
  ///
  /// In ar, this message translates to:
  /// **'إعادة إرسال الرمز'**
  String get resendCode;

  /// Home tab
  ///
  /// In ar, this message translates to:
  /// **'الرئيسية'**
  String get home;

  /// Workouts tab
  ///
  /// In ar, this message translates to:
  /// **'التمارين'**
  String get workouts;

  /// Progress tab
  ///
  /// In ar, this message translates to:
  /// **'التقدم'**
  String get progress;

  /// Schedule tab
  ///
  /// In ar, this message translates to:
  /// **'الجدول'**
  String get schedule;

  /// More tab
  ///
  /// In ar, this message translates to:
  /// **'المزيد'**
  String get more;

  /// Clients tab (trainer)
  ///
  /// In ar, this message translates to:
  /// **'العملاء'**
  String get clients;

  /// Programs tab
  ///
  /// In ar, this message translates to:
  /// **'البرامج'**
  String get programs;

  /// Dashboard page title
  ///
  /// In ar, this message translates to:
  /// **'لوحة التحكم'**
  String get dashboard;

  /// Chat tab
  ///
  /// In ar, this message translates to:
  /// **'الدردشة'**
  String get chat;

  /// Nutrition tab
  ///
  /// In ar, this message translates to:
  /// **'التغذية'**
  String get nutrition;

  /// Billing tab
  ///
  /// In ar, this message translates to:
  /// **'الفواتير'**
  String get billing;

  /// Morning greeting
  ///
  /// In ar, this message translates to:
  /// **'صباح الخير'**
  String get goodMorning;

  /// Afternoon greeting
  ///
  /// In ar, this message translates to:
  /// **'مساء الخير'**
  String get goodAfternoon;

  /// Evening greeting
  ///
  /// In ar, this message translates to:
  /// **'مساء الخير'**
  String get goodEvening;

  /// Greeting with user name
  ///
  /// In ar, this message translates to:
  /// **'أهلاً {name}'**
  String greetingWithName(String name);

  /// Today's workout card title
  ///
  /// In ar, this message translates to:
  /// **'تمرين اليوم'**
  String get todaysWorkout;

  /// Next session card title
  ///
  /// In ar, this message translates to:
  /// **'الجلسة القادمة'**
  String get nextSession;

  /// Streak days label
  ///
  /// In ar, this message translates to:
  /// **'أيام متتالية'**
  String get streakDays;

  /// Streak count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا أيام متتالية} =1{يوم واحد متتالي} =2{يومان متتاليان} few{{count} أيام متتالية} other{{count} يوم متتالي}}'**
  String streakCount(int count);

  /// Quick actions section title
  ///
  /// In ar, this message translates to:
  /// **'إجراءات سريعة'**
  String get quickActions;

  /// Recent activity section
  ///
  /// In ar, this message translates to:
  /// **'النشاط الأخير'**
  String get recentActivity;

  /// Start workout button
  ///
  /// In ar, this message translates to:
  /// **'ابدأ التمرين'**
  String get startWorkout;

  /// View all link
  ///
  /// In ar, this message translates to:
  /// **'عرض الكل'**
  String get viewAll;

  /// No workout scheduled today
  ///
  /// In ar, this message translates to:
  /// **'لا يوجد تمرين مجدول اليوم'**
  String get noWorkoutToday;

  /// Upcoming classes section
  ///
  /// In ar, this message translates to:
  /// **'الحصص القادمة'**
  String get upcomingClasses;

  /// Exercise library page title
  ///
  /// In ar, this message translates to:
  /// **'مكتبة التمارين'**
  String get exerciseLibrary;

  /// Build workout button
  ///
  /// In ar, this message translates to:
  /// **'بناء تمرين'**
  String get buildWorkout;

  /// Workout templates page
  ///
  /// In ar, this message translates to:
  /// **'قوالب التمارين'**
  String get workoutTemplates;

  /// Sets label
  ///
  /// In ar, this message translates to:
  /// **'مجموعات'**
  String get sets;

  /// Reps label
  ///
  /// In ar, this message translates to:
  /// **'تكرارات'**
  String get reps;

  /// Weight label
  ///
  /// In ar, this message translates to:
  /// **'وزن'**
  String get weight;

  /// Rest period label
  ///
  /// In ar, this message translates to:
  /// **'راحة'**
  String get rest;

  /// Duration label
  ///
  /// In ar, this message translates to:
  /// **'المدة'**
  String get duration;

  /// Distance label
  ///
  /// In ar, this message translates to:
  /// **'المسافة'**
  String get distance;

  /// Exercise name field
  ///
  /// In ar, this message translates to:
  /// **'اسم التمرين'**
  String get exerciseName;

  /// Muscle group filter
  ///
  /// In ar, this message translates to:
  /// **'المجموعة العضلية'**
  String get muscleGroup;

  /// Chest muscle group
  ///
  /// In ar, this message translates to:
  /// **'صدر'**
  String get chest;

  /// Back button
  ///
  /// In ar, this message translates to:
  /// **'رجوع'**
  String get back;

  /// Shoulders muscle group
  ///
  /// In ar, this message translates to:
  /// **'أكتاف'**
  String get shoulders;

  /// Biceps muscle group
  ///
  /// In ar, this message translates to:
  /// **'باي'**
  String get biceps;

  /// Triceps muscle group
  ///
  /// In ar, this message translates to:
  /// **'تراي'**
  String get triceps;

  /// Legs muscle group
  ///
  /// In ar, this message translates to:
  /// **'أرجل'**
  String get legs;

  /// Abs muscle group
  ///
  /// In ar, this message translates to:
  /// **'بطن'**
  String get abs;

  /// Cardio category
  ///
  /// In ar, this message translates to:
  /// **'كارديو'**
  String get cardio;

  /// Full body category
  ///
  /// In ar, this message translates to:
  /// **'جسم كامل'**
  String get fullBody;

  /// Workout name field
  ///
  /// In ar, this message translates to:
  /// **'اسم التمرين'**
  String get workoutName;

  /// Add exercise button
  ///
  /// In ar, this message translates to:
  /// **'إضافة تمرين'**
  String get addExercise;

  /// Remove exercise button
  ///
  /// In ar, this message translates to:
  /// **'حذف التمرين'**
  String get removeExercise;

  /// Reorder exercises
  ///
  /// In ar, this message translates to:
  /// **'ترتيب التمارين'**
  String get reorderExercises;

  /// Super set label
  ///
  /// In ar, this message translates to:
  /// **'سوبر سِت'**
  String get superSet;

  /// Drop set label
  ///
  /// In ar, this message translates to:
  /// **'دروب سِت'**
  String get dropSet;

  /// Warm-up section
  ///
  /// In ar, this message translates to:
  /// **'إحماء'**
  String get warmUp;

  /// Cool-down section
  ///
  /// In ar, this message translates to:
  /// **'تهدئة'**
  String get coolDown;

  /// Personal record notification
  ///
  /// In ar, this message translates to:
  /// **'رقم شخصي جديد!'**
  String get personalRecord;

  /// Workout complete message
  ///
  /// In ar, this message translates to:
  /// **'تم إنهاء التمرين!'**
  String get workoutComplete;

  /// Total volume stat
  ///
  /// In ar, this message translates to:
  /// **'الحجم الكلي'**
  String get totalVolume;

  /// Workout duration stat
  ///
  /// In ar, this message translates to:
  /// **'مدة التمرين'**
  String get workoutDuration;

  /// Assign workout to client
  ///
  /// In ar, this message translates to:
  /// **'تعيين تمرين'**
  String get assignWorkout;

  /// Assign program to client
  ///
  /// In ar, this message translates to:
  /// **'تعيين برنامج'**
  String get assignProgram;

  /// Program name field
  ///
  /// In ar, this message translates to:
  /// **'اسم البرنامج'**
  String get programName;

  /// Program weeks field
  ///
  /// In ar, this message translates to:
  /// **'عدد الأسابيع'**
  String get programWeeks;

  /// Days per week field
  ///
  /// In ar, this message translates to:
  /// **'أيام في الأسبوع'**
  String get daysPerWeek;

  /// Remote workout label
  ///
  /// In ar, this message translates to:
  /// **'تمرين عن بُعد'**
  String get remoteWorkout;

  /// Remote workout enabled status
  ///
  /// In ar, this message translates to:
  /// **'التمرين عن بُعد مُفعّل'**
  String get remoteWorkoutEnabled;

  /// Remote workout disabled status
  ///
  /// In ar, this message translates to:
  /// **'التمرين عن بُعد مُعطّل'**
  String get remoteWorkoutDisabled;

  /// Remote workout activation reason
  ///
  /// In ar, this message translates to:
  /// **'سبب التفعيل'**
  String get remoteReason;

  /// Workout history page
  ///
  /// In ar, this message translates to:
  /// **'سجل التمارين'**
  String get workoutHistory;

  /// Empty state for workouts
  ///
  /// In ar, this message translates to:
  /// **'لا توجد تمارين بعد'**
  String get noWorkoutsYet;

  /// Body weight metric
  ///
  /// In ar, this message translates to:
  /// **'الوزن'**
  String get bodyWeight;

  /// Body fat percentage
  ///
  /// In ar, this message translates to:
  /// **'نسبة الدهون'**
  String get bodyFat;

  /// Body measurements page
  ///
  /// In ar, this message translates to:
  /// **'القياسات'**
  String get measurements;

  /// Progress photos page
  ///
  /// In ar, this message translates to:
  /// **'صور التقدم'**
  String get progressPhotos;

  /// Compare button
  ///
  /// In ar, this message translates to:
  /// **'مقارنة'**
  String get compare;

  /// Add measurement button
  ///
  /// In ar, this message translates to:
  /// **'إضافة قياس'**
  String get addMeasurement;

  /// Waist measurement
  ///
  /// In ar, this message translates to:
  /// **'الخصر'**
  String get waist;

  /// Hips measurement
  ///
  /// In ar, this message translates to:
  /// **'الأرداف'**
  String get hips;

  /// Chest measurement
  ///
  /// In ar, this message translates to:
  /// **'الصدر'**
  String get chestMeasurement;

  /// Left arm measurement
  ///
  /// In ar, this message translates to:
  /// **'الذراع الأيسر'**
  String get armLeft;

  /// Right arm measurement
  ///
  /// In ar, this message translates to:
  /// **'الذراع الأيمن'**
  String get armRight;

  /// Left thigh measurement
  ///
  /// In ar, this message translates to:
  /// **'الفخذ الأيسر'**
  String get thighLeft;

  /// Right thigh measurement
  ///
  /// In ar, this message translates to:
  /// **'الفخذ الأيمن'**
  String get thighRight;

  /// Take photo button
  ///
  /// In ar, this message translates to:
  /// **'التقاط صورة'**
  String get takePhoto;

  /// Choose from gallery
  ///
  /// In ar, this message translates to:
  /// **'اختيار من المعرض'**
  String get chooseFromGallery;

  /// Front pose
  ///
  /// In ar, this message translates to:
  /// **'أمامي'**
  String get front;

  /// Side pose
  ///
  /// In ar, this message translates to:
  /// **'جانبي'**
  String get side;

  /// Back pose
  ///
  /// In ar, this message translates to:
  /// **'خلفي'**
  String get backPose;

  /// Progress chart title
  ///
  /// In ar, this message translates to:
  /// **'مخطط التقدم'**
  String get progressChart;

  /// Empty state for progress
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بيانات تقدم بعد'**
  String get noProgressData;

  /// Goal label
  ///
  /// In ar, this message translates to:
  /// **'الهدف'**
  String get goal;

  /// Current value label
  ///
  /// In ar, this message translates to:
  /// **'الحالي'**
  String get current;

  /// Change/difference label
  ///
  /// In ar, this message translates to:
  /// **'التغيير'**
  String get change;

  /// Book session button
  ///
  /// In ar, this message translates to:
  /// **'حجز جلسة'**
  String get bookSession;

  /// Cancel session button
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الجلسة'**
  String get cancelSession;

  /// Cancel booking button
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الحجز'**
  String get cancelBooking;

  /// Waiting list label
  ///
  /// In ar, this message translates to:
  /// **'قائمة الانتظار'**
  String get waitingList;

  /// Group classes page title
  ///
  /// In ar, this message translates to:
  /// **'الحصص الجماعية'**
  String get groupClasses;

  /// Available slots label
  ///
  /// In ar, this message translates to:
  /// **'الأماكن المتاحة'**
  String get availableSlots;

  /// Fully booked status
  ///
  /// In ar, this message translates to:
  /// **'مكتمل'**
  String get fullyBooked;

  /// Join waiting list button
  ///
  /// In ar, this message translates to:
  /// **'الانضمام لقائمة الانتظار'**
  String get joinWaitingList;

  /// Session details page
  ///
  /// In ar, this message translates to:
  /// **'تفاصيل الجلسة'**
  String get sessionDetails;

  /// Session date field
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الجلسة'**
  String get sessionDate;

  /// Session time field
  ///
  /// In ar, this message translates to:
  /// **'وقت الجلسة'**
  String get sessionTime;

  /// Session type field
  ///
  /// In ar, this message translates to:
  /// **'نوع الجلسة'**
  String get sessionType;

  /// Private/PT session
  ///
  /// In ar, this message translates to:
  /// **'جلسة خاصة'**
  String get privateSession;

  /// Group session
  ///
  /// In ar, this message translates to:
  /// **'جلسة جماعية'**
  String get groupSession;

  /// Confirmed status
  ///
  /// In ar, this message translates to:
  /// **'مؤكد'**
  String get confirmed;

  /// Pending status
  ///
  /// In ar, this message translates to:
  /// **'قيد الانتظار'**
  String get pending;

  /// Cancelled status
  ///
  /// In ar, this message translates to:
  /// **'ملغي'**
  String get cancelled;

  /// Completed status
  ///
  /// In ar, this message translates to:
  /// **'مكتمل'**
  String get completed;

  /// No-show status
  ///
  /// In ar, this message translates to:
  /// **'لم يحضر'**
  String get noShow;

  /// Late cancellation status
  ///
  /// In ar, this message translates to:
  /// **'إلغاء متأخر'**
  String get lateCancel;

  /// Late cancel policy warning
  ///
  /// In ar, this message translates to:
  /// **'سيتم خصم جلسة من باقتك عند الإلغاء المتأخر'**
  String get lateCancelWarning;

  /// Cancellation window message
  ///
  /// In ar, this message translates to:
  /// **'يجب الإلغاء قبل {hours} ساعة على الأقل'**
  String cancelWindowHours(int hours);

  /// Trainer availability page
  ///
  /// In ar, this message translates to:
  /// **'أوقات المدرب المتاحة'**
  String get trainerAvailability;

  /// Set availability button
  ///
  /// In ar, this message translates to:
  /// **'تعيين الأوقات المتاحة'**
  String get setAvailability;

  /// Empty state for sessions
  ///
  /// In ar, this message translates to:
  /// **'لا توجد جلسات مجدولة'**
  String get noSessionsScheduled;

  /// Live session mode for trainers
  ///
  /// In ar, this message translates to:
  /// **'وضع الجلسة الحية'**
  String get liveSessionMode;

  /// Start session button
  ///
  /// In ar, this message translates to:
  /// **'بدء الجلسة'**
  String get startSession;

  /// End session button
  ///
  /// In ar, this message translates to:
  /// **'إنهاء الجلسة'**
  String get endSession;

  /// Session notes field
  ///
  /// In ar, this message translates to:
  /// **'ملاحظات الجلسة'**
  String get sessionNotes;

  /// Trainer notes field
  ///
  /// In ar, this message translates to:
  /// **'ملاحظات المدرب'**
  String get trainerNotes;

  /// Food log page
  ///
  /// In ar, this message translates to:
  /// **'سجل الطعام'**
  String get foodLog;

  /// Water tracking page
  ///
  /// In ar, this message translates to:
  /// **'تتبع المياه'**
  String get waterTracking;

  /// Meal plan page
  ///
  /// In ar, this message translates to:
  /// **'خطة الوجبات'**
  String get mealPlan;

  /// Add food button
  ///
  /// In ar, this message translates to:
  /// **'إضافة طعام'**
  String get addFood;

  /// Search food field
  ///
  /// In ar, this message translates to:
  /// **'بحث عن طعام'**
  String get searchFood;

  /// Calories label
  ///
  /// In ar, this message translates to:
  /// **'سعرات حرارية'**
  String get calories;

  /// Protein label
  ///
  /// In ar, this message translates to:
  /// **'بروتين'**
  String get protein;

  /// Carbs label
  ///
  /// In ar, this message translates to:
  /// **'كربوهيدرات'**
  String get carbs;

  /// Fat label
  ///
  /// In ar, this message translates to:
  /// **'دهون'**
  String get fat;

  /// Fiber label
  ///
  /// In ar, this message translates to:
  /// **'ألياف'**
  String get fiber;

  /// Breakfast meal
  ///
  /// In ar, this message translates to:
  /// **'فطور'**
  String get breakfast;

  /// Lunch meal
  ///
  /// In ar, this message translates to:
  /// **'غداء'**
  String get lunch;

  /// Dinner meal
  ///
  /// In ar, this message translates to:
  /// **'عشاء'**
  String get dinner;

  /// Snack
  ///
  /// In ar, this message translates to:
  /// **'وجبة خفيفة'**
  String get snack;

  /// Serving size field
  ///
  /// In ar, this message translates to:
  /// **'حجم الحصة'**
  String get servingSize;

  /// Gram unit
  ///
  /// In ar, this message translates to:
  /// **'جرام'**
  String get gram;

  /// Cup unit
  ///
  /// In ar, this message translates to:
  /// **'كوب'**
  String get cup;

  /// Piece unit
  ///
  /// In ar, this message translates to:
  /// **'قطعة'**
  String get piece;

  /// Daily goal label
  ///
  /// In ar, this message translates to:
  /// **'الهدف اليومي'**
  String get dailyGoal;

  /// Water cups with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لم تشرب ماء} =1{كوب واحد} =2{كوبان} few{{count} أكواب} other{{count} كوب}}'**
  String waterCups(int count);

  /// Add water button
  ///
  /// In ar, this message translates to:
  /// **'إضافة ماء'**
  String get addWater;

  /// Remaining label
  ///
  /// In ar, this message translates to:
  /// **'المتبقي'**
  String get remaining;

  /// Consumed label
  ///
  /// In ar, this message translates to:
  /// **'المستهلك'**
  String get consumed;

  /// Macronutrients label
  ///
  /// In ar, this message translates to:
  /// **'المغذيات الكبرى'**
  String get macros;

  /// Empty state for meal plan
  ///
  /// In ar, this message translates to:
  /// **'لا توجد خطة وجبات'**
  String get noMealPlan;

  /// Conversations page
  ///
  /// In ar, this message translates to:
  /// **'المحادثات'**
  String get conversations;

  /// Message input placeholder
  ///
  /// In ar, this message translates to:
  /// **'اكتب رسالة...'**
  String get typeMessage;

  /// Send message button
  ///
  /// In ar, this message translates to:
  /// **'إرسال'**
  String get sendMessage;

  /// Announcements page
  ///
  /// In ar, this message translates to:
  /// **'الإعلانات'**
  String get announcements;

  /// Notifications page
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات'**
  String get notifications;

  /// Mark all as read
  ///
  /// In ar, this message translates to:
  /// **'تحديد الكل كمقروء'**
  String get markAllRead;

  /// Empty state for notifications
  ///
  /// In ar, this message translates to:
  /// **'لا توجد إشعارات'**
  String get noNotifications;

  /// Empty state for conversations
  ///
  /// In ar, this message translates to:
  /// **'لا توجد محادثات'**
  String get noConversations;

  /// New announcement button
  ///
  /// In ar, this message translates to:
  /// **'إعلان جديد'**
  String get newAnnouncement;

  /// Announcement title field
  ///
  /// In ar, this message translates to:
  /// **'عنوان الإعلان'**
  String get announcementTitle;

  /// Announcement body field
  ///
  /// In ar, this message translates to:
  /// **'نص الإعلان'**
  String get announcementBody;

  /// Notification preferences page
  ///
  /// In ar, this message translates to:
  /// **'تفضيلات الإشعارات'**
  String get notificationPreferences;

  /// Push notifications toggle
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات الفورية'**
  String get pushNotifications;

  /// Email notifications toggle
  ///
  /// In ar, this message translates to:
  /// **'إشعارات البريد الإلكتروني'**
  String get emailNotifications;

  /// Record payment button
  ///
  /// In ar, this message translates to:
  /// **'تسجيل دفعة'**
  String get recordPayment;

  /// Packages page
  ///
  /// In ar, this message translates to:
  /// **'الباقات'**
  String get packages;

  /// Receipt label
  ///
  /// In ar, this message translates to:
  /// **'إيصال'**
  String get receipt;

  /// Cash payment method
  ///
  /// In ar, this message translates to:
  /// **'نقدي'**
  String get cash;

  /// Card payment method
  ///
  /// In ar, this message translates to:
  /// **'بطاقة'**
  String get card;

  /// Bank transfer payment method
  ///
  /// In ar, this message translates to:
  /// **'تحويل بنكي'**
  String get bankTransfer;

  /// Payment amount field
  ///
  /// In ar, this message translates to:
  /// **'المبلغ'**
  String get paymentAmount;

  /// Payment date field
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الدفع'**
  String get paymentDate;

  /// Payment status label
  ///
  /// In ar, this message translates to:
  /// **'حالة الدفع'**
  String get paymentStatus;

  /// Paid status
  ///
  /// In ar, this message translates to:
  /// **'مدفوع'**
  String get paid;

  /// Voided payment status
  ///
  /// In ar, this message translates to:
  /// **'ملغي'**
  String get voided;

  /// Overdue status
  ///
  /// In ar, this message translates to:
  /// **'متأخر'**
  String get overdue;

  /// Membership plan label
  ///
  /// In ar, this message translates to:
  /// **'خطة العضوية'**
  String get membershipPlan;

  /// Renew membership button
  ///
  /// In ar, this message translates to:
  /// **'تجديد العضوية'**
  String get renewMembership;

  /// Membership expiry date
  ///
  /// In ar, this message translates to:
  /// **'تاريخ انتهاء العضوية'**
  String get membershipExpiry;

  /// Membership expired status
  ///
  /// In ar, this message translates to:
  /// **'العضوية منتهية'**
  String get membershipExpired;

  /// Membership active status
  ///
  /// In ar, this message translates to:
  /// **'العضوية نشطة'**
  String get membershipActive;

  /// Session package label
  ///
  /// In ar, this message translates to:
  /// **'باقة الجلسات'**
  String get sessionPackage;

  /// Remaining sessions label
  ///
  /// In ar, this message translates to:
  /// **'الجلسات المتبقية'**
  String get remainingSessions;

  /// Sessions count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا جلسات متبقية} =1{جلسة واحدة متبقية} =2{جلستان متبقيتان} few{{count} جلسات متبقية} other{{count} جلسة متبقية}}'**
  String sessionsCount(int count);

  /// View receipt button
  ///
  /// In ar, this message translates to:
  /// **'عرض الإيصال'**
  String get viewReceipt;

  /// Payment history page
  ///
  /// In ar, this message translates to:
  /// **'سجل المدفوعات'**
  String get paymentHistory;

  /// Empty state for payments
  ///
  /// In ar, this message translates to:
  /// **'لا توجد مدفوعات'**
  String get noPayments;

  /// Egyptian pound abbreviation
  ///
  /// In ar, this message translates to:
  /// **'ج.م'**
  String get egp;

  /// Leaderboard page
  ///
  /// In ar, this message translates to:
  /// **'لوحة المتصدرين'**
  String get leaderboard;

  /// Trainer rating label
  ///
  /// In ar, this message translates to:
  /// **'تقييم المدرب'**
  String get trainerRating;

  /// Trainer of the month title
  ///
  /// In ar, this message translates to:
  /// **'مدرب الشهر'**
  String get trainerOfTheMonth;

  /// Certificates page
  ///
  /// In ar, this message translates to:
  /// **'شهادات'**
  String get certificates;

  /// Badges label
  ///
  /// In ar, this message translates to:
  /// **'الأوسمة'**
  String get badges;

  /// Points label
  ///
  /// In ar, this message translates to:
  /// **'النقاط'**
  String get points;

  /// Rank label
  ///
  /// In ar, this message translates to:
  /// **'الترتيب'**
  String get rank;

  /// Rate trainer button
  ///
  /// In ar, this message translates to:
  /// **'قيّم المدرب'**
  String get rateTrainer;

  /// Rating submitted confirmation
  ///
  /// In ar, this message translates to:
  /// **'تم إرسال التقييم'**
  String get ratingSubmitted;

  /// Opt out of leaderboard
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الاشتراك في لوحة المتصدرين'**
  String get optOutLeaderboard;

  /// Opt into leaderboard
  ///
  /// In ar, this message translates to:
  /// **'الاشتراك في لوحة المتصدرين'**
  String get optInLeaderboard;

  /// Trainer public profile
  ///
  /// In ar, this message translates to:
  /// **'ملف المدرب'**
  String get trainerProfile;

  /// Monthly score label
  ///
  /// In ar, this message translates to:
  /// **'النتيجة الشهرية'**
  String get monthlyScore;

  /// Empty state for leaderboard
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بيانات في لوحة المتصدرين'**
  String get noLeaderboardData;

  /// Check-in action
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الحضور'**
  String get checkIn;

  /// Member number field
  ///
  /// In ar, this message translates to:
  /// **'رقم العضو'**
  String get memberNumber;

  /// Scan QR code button
  ///
  /// In ar, this message translates to:
  /// **'مسح QR'**
  String get scanQR;

  /// Welcome message at check-in
  ///
  /// In ar, this message translates to:
  /// **'مرحبًا بك في {gymName}'**
  String welcomeTo(String gymName);

  /// Membership expired at check-in
  ///
  /// In ar, this message translates to:
  /// **'عضويتك منتهية'**
  String get membershipExpiredCheckIn;

  /// Check-in success message
  ///
  /// In ar, this message translates to:
  /// **'تم تسجيل الحضور بنجاح'**
  String get checkInSuccess;

  /// Check-in failure message
  ///
  /// In ar, this message translates to:
  /// **'فشل تسجيل الحضور'**
  String get checkInFailed;

  /// Kiosk mode label
  ///
  /// In ar, this message translates to:
  /// **'وضع الكيوسك'**
  String get kioskMode;

  /// Enter member number prompt in kiosk
  ///
  /// In ar, this message translates to:
  /// **'أدخل رقم العضو'**
  String get enterMemberNumber;

  /// Numpad entry mode
  ///
  /// In ar, this message translates to:
  /// **'إدخال رقمي'**
  String get numpadEntry;

  /// QR entry mode
  ///
  /// In ar, this message translates to:
  /// **'مسح QR'**
  String get qrEntry;

  /// Staff page
  ///
  /// In ar, this message translates to:
  /// **'الموظفين'**
  String get staff;

  /// Owner role
  ///
  /// In ar, this message translates to:
  /// **'المالك'**
  String get owner;

  /// Manager role
  ///
  /// In ar, this message translates to:
  /// **'المدير'**
  String get manager;

  /// Receptionist role
  ///
  /// In ar, this message translates to:
  /// **'موظف الاستقبال'**
  String get receptionist;

  /// Trainer role
  ///
  /// In ar, this message translates to:
  /// **'مدرب'**
  String get trainer;

  /// Member role
  ///
  /// In ar, this message translates to:
  /// **'عضو'**
  String get member;

  /// Admin role
  ///
  /// In ar, this message translates to:
  /// **'مسؤول'**
  String get admin;

  /// Add staff button
  ///
  /// In ar, this message translates to:
  /// **'إضافة موظف'**
  String get addStaff;

  /// Edit staff
  ///
  /// In ar, this message translates to:
  /// **'تعديل موظف'**
  String get editStaff;

  /// Remove staff
  ///
  /// In ar, this message translates to:
  /// **'حذف موظف'**
  String get removeStaff;

  /// Staff role field
  ///
  /// In ar, this message translates to:
  /// **'دور الموظف'**
  String get staffRole;

  /// Settings page
  ///
  /// In ar, this message translates to:
  /// **'الإعدادات'**
  String get settings;

  /// Language setting
  ///
  /// In ar, this message translates to:
  /// **'اللغة'**
  String get language;

  /// Arabic language
  ///
  /// In ar, this message translates to:
  /// **'العربية'**
  String get arabic;

  /// English language
  ///
  /// In ar, this message translates to:
  /// **'English'**
  String get english;

  /// Units setting
  ///
  /// In ar, this message translates to:
  /// **'الوحدات'**
  String get units;

  /// Kilogram unit
  ///
  /// In ar, this message translates to:
  /// **'كيلوجرام'**
  String get kg;

  /// Pound unit
  ///
  /// In ar, this message translates to:
  /// **'رطل'**
  String get lb;

  /// Centimeter unit
  ///
  /// In ar, this message translates to:
  /// **'سنتيمتر'**
  String get cm;

  /// Inch unit
  ///
  /// In ar, this message translates to:
  /// **'إنش'**
  String get inch;

  /// PDPL data download
  ///
  /// In ar, this message translates to:
  /// **'تحميل بياناتي'**
  String get downloadMyData;

  /// PDPL account deletion
  ///
  /// In ar, this message translates to:
  /// **'حذف حسابي'**
  String get deleteMyAccount;

  /// Dark mode toggle
  ///
  /// In ar, this message translates to:
  /// **'الوضع الداكن'**
  String get darkMode;

  /// Light mode toggle
  ///
  /// In ar, this message translates to:
  /// **'الوضع الفاتح'**
  String get lightMode;

  /// System theme option
  ///
  /// In ar, this message translates to:
  /// **'حسب النظام'**
  String get systemTheme;

  /// App version label
  ///
  /// In ar, this message translates to:
  /// **'إصدار التطبيق'**
  String get appVersion;

  /// Terms of service link
  ///
  /// In ar, this message translates to:
  /// **'شروط الخدمة'**
  String get termsOfService;

  /// Privacy policy link
  ///
  /// In ar, this message translates to:
  /// **'سياسة الخصوصية'**
  String get privacyPolicy;

  /// About us page
  ///
  /// In ar, this message translates to:
  /// **'عن التطبيق'**
  String get aboutUs;

  /// Contact support link
  ///
  /// In ar, this message translates to:
  /// **'التواصل مع الدعم'**
  String get contactSupport;

  /// Profile page
  ///
  /// In ar, this message translates to:
  /// **'الملف الشخصي'**
  String get profile;

  /// Edit profile button
  ///
  /// In ar, this message translates to:
  /// **'تعديل الملف الشخصي'**
  String get editProfile;

  /// Change password
  ///
  /// In ar, this message translates to:
  /// **'تغيير كلمة المرور'**
  String get changePassword;

  /// Current password field
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور الحالية'**
  String get currentPassword;

  /// New password field
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور الجديدة'**
  String get newPassword;

  /// Free plan name
  ///
  /// In ar, this message translates to:
  /// **'الخطة المجانية'**
  String get freePlan;

  /// Starter plan name
  ///
  /// In ar, this message translates to:
  /// **'المبتدئ'**
  String get starterPlan;

  /// Pro plan name
  ///
  /// In ar, this message translates to:
  /// **'الاحترافي'**
  String get proPlan;

  /// Business plan name
  ///
  /// In ar, this message translates to:
  /// **'الأعمال'**
  String get businessPlan;

  /// Upgrade button
  ///
  /// In ar, this message translates to:
  /// **'ترقية'**
  String get upgrade;

  /// Feature gating message
  ///
  /// In ar, this message translates to:
  /// **'ميزة غير متاحة في خطتك الحالية'**
  String get featureNotAvailable;

  /// Upgrade prompt
  ///
  /// In ar, this message translates to:
  /// **'قم بالترقية لفتح هذه الميزة'**
  String get upgradeToUnlock;

  /// Current plan label
  ///
  /// In ar, this message translates to:
  /// **'الخطة الحالية'**
  String get currentPlan;

  /// Plan features list
  ///
  /// In ar, this message translates to:
  /// **'مميزات الخطة'**
  String get planFeatures;

  /// Compare plans page
  ///
  /// In ar, this message translates to:
  /// **'مقارنة الخطط'**
  String get comparePlans;

  /// Storage limit reached message
  ///
  /// In ar, this message translates to:
  /// **'تم الوصول لحد التخزين'**
  String get storageLimitReached;

  /// Storage used message
  ///
  /// In ar, this message translates to:
  /// **'التخزين المستخدم: {used} من {total}'**
  String storageUsed(String used, String total);

  /// Save button
  ///
  /// In ar, this message translates to:
  /// **'حفظ'**
  String get save;

  /// Cancel button
  ///
  /// In ar, this message translates to:
  /// **'إلغاء'**
  String get cancel;

  /// Confirm button
  ///
  /// In ar, this message translates to:
  /// **'تأكيد'**
  String get confirm;

  /// Delete button
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get delete;

  /// Edit button
  ///
  /// In ar, this message translates to:
  /// **'تعديل'**
  String get edit;

  /// Add button
  ///
  /// In ar, this message translates to:
  /// **'إضافة'**
  String get add;

  /// Search button/field
  ///
  /// In ar, this message translates to:
  /// **'بحث'**
  String get search;

  /// Filter button
  ///
  /// In ar, this message translates to:
  /// **'تصفية'**
  String get filter;

  /// Sort button
  ///
  /// In ar, this message translates to:
  /// **'ترتيب'**
  String get sort;

  /// Loading indicator
  ///
  /// In ar, this message translates to:
  /// **'تحميل...'**
  String get loading;

  /// Error label
  ///
  /// In ar, this message translates to:
  /// **'خطأ'**
  String get error;

  /// Empty state
  ///
  /// In ar, this message translates to:
  /// **'فارغ'**
  String get empty;

  /// Retry button
  ///
  /// In ar, this message translates to:
  /// **'إعادة المحاولة'**
  String get retry;

  /// OK button
  ///
  /// In ar, this message translates to:
  /// **'حسنًا'**
  String get ok;

  /// Yes
  ///
  /// In ar, this message translates to:
  /// **'نعم'**
  String get yes;

  /// No
  ///
  /// In ar, this message translates to:
  /// **'لا'**
  String get no;

  /// Close button
  ///
  /// In ar, this message translates to:
  /// **'إغلاق'**
  String get close;

  /// Done button
  ///
  /// In ar, this message translates to:
  /// **'تم'**
  String get done;

  /// Next button
  ///
  /// In ar, this message translates to:
  /// **'التالي'**
  String get next;

  /// Previous button
  ///
  /// In ar, this message translates to:
  /// **'السابق'**
  String get previous;

  /// Submit button
  ///
  /// In ar, this message translates to:
  /// **'إرسال'**
  String get submit;

  /// Refresh button
  ///
  /// In ar, this message translates to:
  /// **'تحديث'**
  String get refresh;

  /// Share button
  ///
  /// In ar, this message translates to:
  /// **'مشاركة'**
  String get share;

  /// Copy button
  ///
  /// In ar, this message translates to:
  /// **'نسخ'**
  String get copy;

  /// Select all
  ///
  /// In ar, this message translates to:
  /// **'تحديد الكل'**
  String get selectAll;

  /// Deselect all
  ///
  /// In ar, this message translates to:
  /// **'إلغاء التحديد'**
  String get deselectAll;

  /// Required field indicator
  ///
  /// In ar, this message translates to:
  /// **'مطلوب'**
  String get required;

  /// Optional field indicator
  ///
  /// In ar, this message translates to:
  /// **'اختياري'**
  String get optional;

  /// No results found
  ///
  /// In ar, this message translates to:
  /// **'لا توجد نتائج'**
  String get noResults;

  /// Pull to refresh
  ///
  /// In ar, this message translates to:
  /// **'اسحب للتحديث'**
  String get pullToRefresh;

  /// See more / expand
  ///
  /// In ar, this message translates to:
  /// **'عرض المزيد'**
  String get seeMore;

  /// See less / collapse
  ///
  /// In ar, this message translates to:
  /// **'عرض أقل'**
  String get seeLess;

  /// Coming soon label
  ///
  /// In ar, this message translates to:
  /// **'قريبًا'**
  String get comingSoon;

  /// PDPL data processing consent
  ///
  /// In ar, this message translates to:
  /// **'الموافقة على معالجة البيانات'**
  String get dataProcessingConsent;

  /// PDPL data processing description
  ///
  /// In ar, this message translates to:
  /// **'أوافق على معالجة بياناتي الشخصية وفقًا لقانون حماية البيانات الشخصية المصري'**
  String get dataProcessingConsentDescription;

  /// Marketing consent toggle
  ///
  /// In ar, this message translates to:
  /// **'الموافقة على التسويق'**
  String get marketingConsent;

  /// Marketing consent description
  ///
  /// In ar, this message translates to:
  /// **'أوافق على تلقي رسائل تسويقية وعروض خاصة'**
  String get marketingConsentDescription;

  /// Data download description
  ///
  /// In ar, this message translates to:
  /// **'تحميل نسخة من جميع بياناتك الشخصية المخزنة'**
  String get downloadMyDataDescription;

  /// Account deletion description
  ///
  /// In ar, this message translates to:
  /// **'حذف حسابك وجميع بياناتك نهائيًا. لا يمكن التراجع عن هذا الإجراء.'**
  String get deleteMyAccountDescription;

  /// Confirmation prompt
  ///
  /// In ar, this message translates to:
  /// **'هل أنت متأكد؟'**
  String get areYouSure;

  /// Account deletion confirmation dialog
  ///
  /// In ar, this message translates to:
  /// **'سيتم حذف حسابك وجميع بياناتك بشكل نهائي. هل تريد المتابعة؟'**
  String get deleteAccountConfirmation;

  /// Data export requested confirmation
  ///
  /// In ar, this message translates to:
  /// **'تم طلب تصدير البيانات. ستتلقى إشعارًا عند جهوز الملف.'**
  String get dataExportRequested;

  /// Consent updated confirmation
  ///
  /// In ar, this message translates to:
  /// **'تم تحديث الموافقة'**
  String get consentUpdated;

  /// Privacy settings page
  ///
  /// In ar, this message translates to:
  /// **'إعدادات الخصوصية'**
  String get privacySettings;

  /// Data retention notice
  ///
  /// In ar, this message translates to:
  /// **'سيتم الاحتفاظ ببياناتك لمدة {days} يومًا بعد حذف الحساب وفقًا للقانون'**
  String dataRetentionNotice(int days);

  /// Gym label
  ///
  /// In ar, this message translates to:
  /// **'الجيم'**
  String get gym;

  /// Members page
  ///
  /// In ar, this message translates to:
  /// **'الأعضاء'**
  String get members;

  /// Member count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا أعضاء} =1{عضو واحد} =2{عضوان} few{{count} أعضاء} other{{count} عضو}}'**
  String membersCount(int count);

  /// Trainers page
  ///
  /// In ar, this message translates to:
  /// **'المدربين'**
  String get trainers;

  /// Trainer count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا مدربين} =1{مدرب واحد} =2{مدربان} few{{count} مدربين} other{{count} مدرب}}'**
  String trainersCount(int count);

  /// Classes page
  ///
  /// In ar, this message translates to:
  /// **'الحصص'**
  String get classes;

  /// Class count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا حصص} =1{حصة واحدة} =2{حصتان} few{{count} حصص} other{{count} حصة}}'**
  String classesCount(int count);

  /// Exercises label
  ///
  /// In ar, this message translates to:
  /// **'التمارين'**
  String get exercises;

  /// Invoices page
  ///
  /// In ar, this message translates to:
  /// **'الفواتير'**
  String get invoices;

  /// Add member button
  ///
  /// In ar, this message translates to:
  /// **'إضافة عضو'**
  String get addMember;

  /// Edit member
  ///
  /// In ar, this message translates to:
  /// **'تعديل عضو'**
  String get editMember;

  /// Member details page
  ///
  /// In ar, this message translates to:
  /// **'تفاصيل العضو'**
  String get memberDetails;

  /// Member since date
  ///
  /// In ar, this message translates to:
  /// **'عضو منذ {date}'**
  String memberSince(String date);

  /// Add trainer button
  ///
  /// In ar, this message translates to:
  /// **'إضافة مدرب'**
  String get addTrainer;

  /// Add class button
  ///
  /// In ar, this message translates to:
  /// **'إضافة حصة'**
  String get addClass;

  /// Class capacity field
  ///
  /// In ar, this message translates to:
  /// **'سعة الحصة'**
  String get classCapacity;

  /// Class name field
  ///
  /// In ar, this message translates to:
  /// **'اسم الحصة'**
  String get className;

  /// Class schedule
  ///
  /// In ar, this message translates to:
  /// **'جدول الحصص'**
  String get classSchedule;

  /// Gym info page
  ///
  /// In ar, this message translates to:
  /// **'معلومات الجيم'**
  String get gymInfo;

  /// Gym name field
  ///
  /// In ar, this message translates to:
  /// **'اسم الجيم'**
  String get gymName;

  /// Gym address field
  ///
  /// In ar, this message translates to:
  /// **'عنوان الجيم'**
  String get gymAddress;

  /// Operating hours
  ///
  /// In ar, this message translates to:
  /// **'ساعات العمل'**
  String get operatingHours;

  /// Seasonal hours (Ramadan)
  ///
  /// In ar, this message translates to:
  /// **'ساعات العمل الموسمية'**
  String get seasonalHours;

  /// Gym sections
  ///
  /// In ar, this message translates to:
  /// **'أقسام الجيم'**
  String get gymSections;

  /// Men's section
  ///
  /// In ar, this message translates to:
  /// **'قسم الرجال'**
  String get mensSection;

  /// Women's section
  ///
  /// In ar, this message translates to:
  /// **'قسم النساء'**
  String get womensSection;

  /// Mixed section
  ///
  /// In ar, this message translates to:
  /// **'قسم مختلط'**
  String get mixedSection;

  /// Switch gym button
  ///
  /// In ar, this message translates to:
  /// **'تبديل الجيم'**
  String get switchGym;

  /// Select gym prompt
  ///
  /// In ar, this message translates to:
  /// **'اختيار الجيم'**
  String get selectGym;

  /// Analytics page
  ///
  /// In ar, this message translates to:
  /// **'التحليلات'**
  String get analytics;

  /// Reports page
  ///
  /// In ar, this message translates to:
  /// **'التقارير'**
  String get reports;

  /// Gym suspended title
  ///
  /// In ar, this message translates to:
  /// **'تم تعليق اشتراك الجيم'**
  String get gymSuspended;

  /// Gym suspended message
  ///
  /// In ar, this message translates to:
  /// **'يرجى التواصل مع إدارة الجيم للمزيد من المعلومات'**
  String get gymSuspendedMessage;

  /// Contact gym admin button
  ///
  /// In ar, this message translates to:
  /// **'التواصل مع إدارة الجيم'**
  String get contactGymAdmin;

  /// Account suspended status
  ///
  /// In ar, this message translates to:
  /// **'الحساب معلّق'**
  String get accountSuspended;

  /// Subscription expired
  ///
  /// In ar, this message translates to:
  /// **'الاشتراك منتهي'**
  String get subscriptionExpired;

  /// Generic error message
  ///
  /// In ar, this message translates to:
  /// **'حدث خطأ ما'**
  String get somethingWentWrong;

  /// Member not found error
  ///
  /// In ar, this message translates to:
  /// **'لم يتم العثور على العضو'**
  String get memberNotFound;

  /// Session not available error
  ///
  /// In ar, this message translates to:
  /// **'الجلسة غير متاحة'**
  String get sessionNotAvailable;

  /// No internet connection error
  ///
  /// In ar, this message translates to:
  /// **'لا يوجد اتصال بالإنترنت'**
  String get noInternetConnection;

  /// Server error
  ///
  /// In ar, this message translates to:
  /// **'خطأ في الخادم'**
  String get serverError;

  /// Unauthorized error
  ///
  /// In ar, this message translates to:
  /// **'غير مصرح لك بهذا الإجراء'**
  String get unauthorizedError;

  /// Session expired error
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية الجلسة. يرجى تسجيل الدخول مرة أخرى.'**
  String get sessionExpired;

  /// Invalid input error
  ///
  /// In ar, this message translates to:
  /// **'بيانات غير صحيحة'**
  String get invalidInput;

  /// Email already exists error
  ///
  /// In ar, this message translates to:
  /// **'البريد الإلكتروني مسجل بالفعل'**
  String get emailAlreadyExists;

  /// Weak password error
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور ضعيفة'**
  String get weakPassword;

  /// Password mismatch error
  ///
  /// In ar, this message translates to:
  /// **'كلمات المرور غير متطابقة'**
  String get passwordMismatch;

  /// Field required validation
  ///
  /// In ar, this message translates to:
  /// **'هذا الحقل مطلوب'**
  String get fieldRequired;

  /// Invalid email validation
  ///
  /// In ar, this message translates to:
  /// **'بريد إلكتروني غير صحيح'**
  String get invalidEmail;

  /// Invalid phone validation
  ///
  /// In ar, this message translates to:
  /// **'رقم هاتف غير صحيح'**
  String get invalidPhone;

  /// File too large error
  ///
  /// In ar, this message translates to:
  /// **'حجم الملف كبير جدًا'**
  String get fileTooLarge;

  /// Upload failed error
  ///
  /// In ar, this message translates to:
  /// **'فشل رفع الملف'**
  String get uploadFailed;

  /// Request timeout error
  ///
  /// In ar, this message translates to:
  /// **'انتهت مهلة الطلب'**
  String get timeoutError;

  /// Trainer not found error
  ///
  /// In ar, this message translates to:
  /// **'لم يتم العثور على المدرب'**
  String get trainerNotFound;

  /// Class is full error
  ///
  /// In ar, this message translates to:
  /// **'الحصة مكتملة'**
  String get classFullError;

  /// Already booked error
  ///
  /// In ar, this message translates to:
  /// **'لديك حجز بالفعل'**
  String get alreadyBooked;

  /// Cannot cancel past session
  ///
  /// In ar, this message translates to:
  /// **'لا يمكن إلغاء جلسة سابقة'**
  String get cannotCancelPastSession;

  /// Today
  ///
  /// In ar, this message translates to:
  /// **'اليوم'**
  String get today;

  /// Yesterday
  ///
  /// In ar, this message translates to:
  /// **'أمس'**
  String get yesterday;

  /// Tomorrow
  ///
  /// In ar, this message translates to:
  /// **'غدًا'**
  String get tomorrow;

  /// This week
  ///
  /// In ar, this message translates to:
  /// **'هذا الأسبوع'**
  String get thisWeek;

  /// This month
  ///
  /// In ar, this message translates to:
  /// **'هذا الشهر'**
  String get thisMonth;

  /// Last week
  ///
  /// In ar, this message translates to:
  /// **'الأسبوع الماضي'**
  String get lastWeek;

  /// Last month
  ///
  /// In ar, this message translates to:
  /// **'الشهر الماضي'**
  String get lastMonth;

  /// Minutes ago with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{منذ دقيقة} =2{منذ دقيقتين} few{منذ {count} دقائق} other{منذ {count} دقيقة}}'**
  String minutesAgo(int count);

  /// Hours ago with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{منذ ساعة} =2{منذ ساعتين} few{منذ {count} ساعات} other{منذ {count} ساعة}}'**
  String hoursAgo(int count);

  /// Days ago with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{منذ يوم} =2{منذ يومين} few{منذ {count} أيام} other{منذ {count} يوم}}'**
  String daysAgo(int count);

  /// Just now
  ///
  /// In ar, this message translates to:
  /// **'الآن'**
  String get justNow;

  /// Date label
  ///
  /// In ar, this message translates to:
  /// **'التاريخ'**
  String get date;

  /// Time label
  ///
  /// In ar, this message translates to:
  /// **'الوقت'**
  String get time;

  /// From (time range)
  ///
  /// In ar, this message translates to:
  /// **'من'**
  String get from;

  /// To (time range)
  ///
  /// In ar, this message translates to:
  /// **'إلى'**
  String get to;

  /// AM
  ///
  /// In ar, this message translates to:
  /// **'صباحًا'**
  String get am;

  /// PM
  ///
  /// In ar, this message translates to:
  /// **'مساءً'**
  String get pm;

  /// Saturday
  ///
  /// In ar, this message translates to:
  /// **'السبت'**
  String get saturday;

  /// Sunday
  ///
  /// In ar, this message translates to:
  /// **'الأحد'**
  String get sunday;

  /// Monday
  ///
  /// In ar, this message translates to:
  /// **'الاثنين'**
  String get monday;

  /// Tuesday
  ///
  /// In ar, this message translates to:
  /// **'الثلاثاء'**
  String get tuesday;

  /// Wednesday
  ///
  /// In ar, this message translates to:
  /// **'الأربعاء'**
  String get wednesday;

  /// Thursday
  ///
  /// In ar, this message translates to:
  /// **'الخميس'**
  String get thursday;

  /// Friday
  ///
  /// In ar, this message translates to:
  /// **'الجمعة'**
  String get friday;

  /// Single member label for counts
  ///
  /// In ar, this message translates to:
  /// **'عضو'**
  String get membersLabel;

  /// Single session label for counts
  ///
  /// In ar, this message translates to:
  /// **'جلسة'**
  String get sessionsLabel;

  /// Workout count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا تمارين} =1{تمرين واحد} =2{تمرينان} few{{count} تمارين} other{{count} تمرين}}'**
  String workoutsCount(int count);

  /// Days count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا أيام} =1{يوم واحد} =2{يومان} few{{count} أيام} other{{count} يوم}}'**
  String daysCount(int count);

  /// Sets count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا مجموعات} =1{مجموعة واحدة} =2{مجموعتان} few{{count} مجموعات} other{{count} مجموعة}}'**
  String setsCount(int count);

  /// Reps count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا تكرارات} =1{تكرار واحد} =2{تكراران} few{{count} تكرارات} other{{count} تكرار}}'**
  String repsCount(int count);

  /// Notification count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا إشعارات} =1{إشعار واحد} =2{إشعاران} few{{count} إشعارات} other{{count} إشعار}}'**
  String notificationsCount(int count);

  /// Payment count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا مدفوعات} =1{دفعة واحدة} =2{دفعتان} few{{count} مدفوعات} other{{count} دفعة}}'**
  String paymentsCount(int count);

  /// Photo count with Arabic plurals
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا صور} =1{صورة واحدة} =2{صورتان} few{{count} صور} other{{count} صورة}}'**
  String photosCount(int count);

  /// Earnings page (trainer)
  ///
  /// In ar, this message translates to:
  /// **'الأرباح'**
  String get earnings;

  /// Total earnings label
  ///
  /// In ar, this message translates to:
  /// **'إجمالي الأرباح'**
  String get totalEarnings;

  /// This month earnings
  ///
  /// In ar, this message translates to:
  /// **'أرباح هذا الشهر'**
  String get thisMonthEarnings;

  /// Earnings read-only notice
  ///
  /// In ar, this message translates to:
  /// **'الأرباح للعرض فقط'**
  String get earningsReadOnly;

  /// Client profile page
  ///
  /// In ar, this message translates to:
  /// **'ملف العميل'**
  String get clientProfile;

  /// Client workouts tab
  ///
  /// In ar, this message translates to:
  /// **'تمارين العميل'**
  String get clientWorkouts;

  /// Client progress tab
  ///
  /// In ar, this message translates to:
  /// **'تقدم العميل'**
  String get clientProgress;

  /// Client nutrition tab
  ///
  /// In ar, this message translates to:
  /// **'تغذية العميل'**
  String get clientNutrition;

  /// Client schedule tab
  ///
  /// In ar, this message translates to:
  /// **'جدول العميل'**
  String get clientSchedule;

  /// Client notes tab
  ///
  /// In ar, this message translates to:
  /// **'ملاحظات العميل'**
  String get clientNotes;

  /// Total clients stat
  ///
  /// In ar, this message translates to:
  /// **'إجمالي العملاء'**
  String get totalClients;

  /// Active clients stat
  ///
  /// In ar, this message translates to:
  /// **'العملاء النشطين'**
  String get activeClients;

  /// Sessions today stat
  ///
  /// In ar, this message translates to:
  /// **'جلسات اليوم'**
  String get sessionsToday;

  /// Sessions this week stat
  ///
  /// In ar, this message translates to:
  /// **'جلسات هذا الأسبوع'**
  String get sessionsThisWeek;

  /// Active status
  ///
  /// In ar, this message translates to:
  /// **'نشط'**
  String get active;

  /// Inactive status
  ///
  /// In ar, this message translates to:
  /// **'غير نشط'**
  String get inactive;

  /// Suspended status
  ///
  /// In ar, this message translates to:
  /// **'معلّق'**
  String get suspended;

  /// Expired status
  ///
  /// In ar, this message translates to:
  /// **'منتهي'**
  String get expired;

  /// All filter option
  ///
  /// In ar, this message translates to:
  /// **'الكل'**
  String get all;

  /// Male gender
  ///
  /// In ar, this message translates to:
  /// **'ذكر'**
  String get male;

  /// Female gender
  ///
  /// In ar, this message translates to:
  /// **'أنثى'**
  String get female;

  /// Age label
  ///
  /// In ar, this message translates to:
  /// **'العمر'**
  String get age;

  /// Height label
  ///
  /// In ar, this message translates to:
  /// **'الطول'**
  String get height;

  /// Date of birth field
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الميلاد'**
  String get dateOfBirth;

  /// Gender field
  ///
  /// In ar, this message translates to:
  /// **'الجنس'**
  String get gender;

  /// Status label
  ///
  /// In ar, this message translates to:
  /// **'الحالة'**
  String get status;

  /// Notes field
  ///
  /// In ar, this message translates to:
  /// **'ملاحظات'**
  String get notes;

  /// Description field
  ///
  /// In ar, this message translates to:
  /// **'الوصف'**
  String get description;

  /// Title field
  ///
  /// In ar, this message translates to:
  /// **'العنوان'**
  String get title;

  /// Type field
  ///
  /// In ar, this message translates to:
  /// **'النوع'**
  String get type;

  /// Category field
  ///
  /// In ar, this message translates to:
  /// **'الفئة'**
  String get category;

  /// Total label
  ///
  /// In ar, this message translates to:
  /// **'الإجمالي'**
  String get total;

  /// Average label
  ///
  /// In ar, this message translates to:
  /// **'المتوسط'**
  String get average;

  /// Minimum label
  ///
  /// In ar, this message translates to:
  /// **'الأدنى'**
  String get min;

  /// Maximum label
  ///
  /// In ar, this message translates to:
  /// **'الأعلى'**
  String get max;

  /// Percentage label
  ///
  /// In ar, this message translates to:
  /// **'النسبة'**
  String get percentage;

  /// Offline mode indicator
  ///
  /// In ar, this message translates to:
  /// **'وضع عدم الاتصال'**
  String get offlineMode;

  /// Offline sync notice
  ///
  /// In ar, this message translates to:
  /// **'سيتم مزامنة التغييرات عند الاتصال بالإنترنت'**
  String get offlineChangesWillSync;

  /// Syncing indicator
  ///
  /// In ar, this message translates to:
  /// **'جارٍ المزامنة...'**
  String get syncing;

  /// Sync complete message
  ///
  /// In ar, this message translates to:
  /// **'تمت المزامنة'**
  String get syncComplete;

  /// Sync failed message
  ///
  /// In ar, this message translates to:
  /// **'فشلت المزامنة'**
  String get syncFailed;

  /// Last synced time
  ///
  /// In ar, this message translates to:
  /// **'آخر مزامنة: {time}'**
  String lastSynced(String time);

  /// Config updated notification
  ///
  /// In ar, this message translates to:
  /// **'تم تحديث الإعدادات'**
  String get configUpdated;

  /// New feature available notification
  ///
  /// In ar, this message translates to:
  /// **'ميزة جديدة متاحة!'**
  String get newFeatureAvailable;

  /// App update available
  ///
  /// In ar, this message translates to:
  /// **'تحديث جديد متاح للتطبيق'**
  String get appUpdateAvailable;

  /// Update now button
  ///
  /// In ar, this message translates to:
  /// **'تحديث الآن'**
  String get updateNow;

  /// Later button
  ///
  /// In ar, this message translates to:
  /// **'لاحقًا'**
  String get later;

  /// Generic success message
  ///
  /// In ar, this message translates to:
  /// **'تمت العملية بنجاح'**
  String get successMessage;

  /// Saved successfully message
  ///
  /// In ar, this message translates to:
  /// **'تم الحفظ بنجاح'**
  String get savedSuccessfully;

  /// Deleted successfully message
  ///
  /// In ar, this message translates to:
  /// **'تم الحذف بنجاح'**
  String get deletedSuccessfully;

  /// Copied to clipboard message
  ///
  /// In ar, this message translates to:
  /// **'تم النسخ'**
  String get copied;

  /// Session policies page
  ///
  /// In ar, this message translates to:
  /// **'سياسات الجلسات'**
  String get sessionPolicies;

  /// Cancellation window setting
  ///
  /// In ar, this message translates to:
  /// **'نافذة الإلغاء'**
  String get cancellationWindow;

  /// No-show policy setting
  ///
  /// In ar, this message translates to:
  /// **'سياسة عدم الحضور'**
  String get noShowPolicy;

  /// Late cancel policy setting
  ///
  /// In ar, this message translates to:
  /// **'سياسة الإلغاء المتأخر'**
  String get lateCancelPolicy;

  /// Deduct from package option
  ///
  /// In ar, this message translates to:
  /// **'خصم من الباقة'**
  String get deductFromPackage;

  /// Export as PDF
  ///
  /// In ar, this message translates to:
  /// **'تصدير كـ PDF'**
  String get exportAsPdf;

  /// Print button
  ///
  /// In ar, this message translates to:
  /// **'طباعة'**
  String get print;

  /// Download report
  ///
  /// In ar, this message translates to:
  /// **'تحميل التقرير'**
  String get downloadReport;

  /// Date range filter
  ///
  /// In ar, this message translates to:
  /// **'نطاق التاريخ'**
  String get dateRange;

  /// Start date
  ///
  /// In ar, this message translates to:
  /// **'تاريخ البدء'**
  String get startDate;

  /// End date
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الانتهاء'**
  String get endDate;

  /// Apply filter button
  ///
  /// In ar, this message translates to:
  /// **'تطبيق'**
  String get apply;

  /// Reset filter button
  ///
  /// In ar, this message translates to:
  /// **'إعادة تعيين'**
  String get reset;

  /// No data available
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بيانات'**
  String get noData;

  /// Total members stat
  ///
  /// In ar, this message translates to:
  /// **'إجمالي الأعضاء'**
  String get totalMembers;

  /// New members stat
  ///
  /// In ar, this message translates to:
  /// **'أعضاء جدد'**
  String get newMembers;

  /// Active members stat
  ///
  /// In ar, this message translates to:
  /// **'الأعضاء النشطين'**
  String get activeMembers;

  /// Revenue label
  ///
  /// In ar, this message translates to:
  /// **'الإيرادات'**
  String get revenue;

  /// Attendance label
  ///
  /// In ar, this message translates to:
  /// **'الحضور'**
  String get attendance;

  /// Attendance rate
  ///
  /// In ar, this message translates to:
  /// **'نسبة الحضور'**
  String get attendanceRate;

  /// Check-in count
  ///
  /// In ar, this message translates to:
  /// **'عدد مرات الحضور'**
  String get checkInCount;

  /// Popular classes stat
  ///
  /// In ar, this message translates to:
  /// **'الحصص الأكثر شعبية'**
  String get popularClasses;

  /// Peak hours stat
  ///
  /// In ar, this message translates to:
  /// **'ساعات الذروة'**
  String get peakHours;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
      'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
      'an issue with the localizations generation tool. Please file an issue '
      'on GitHub with a reproducible sample app and the gen-l10n configuration '
      'that was used.');
}
