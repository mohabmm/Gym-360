// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'Gym 360';

  @override
  String get login => 'Login';

  @override
  String get register => 'Register';

  @override
  String get email => 'Email';

  @override
  String get password => 'Password';

  @override
  String get confirmPassword => 'Confirm Password';

  @override
  String get forgotPassword => 'Forgot Password?';

  @override
  String get resetPassword => 'Reset Password';

  @override
  String get signInWithGoogle => 'Sign in with Google';

  @override
  String get signInWithApple => 'Sign in with Apple';

  @override
  String get fullName => 'Full Name';

  @override
  String get phoneNumber => 'Phone Number';

  @override
  String get logout => 'Logout';

  @override
  String get welcomeBack => 'Welcome Back';

  @override
  String get createAccount => 'Create Account';

  @override
  String get orContinueWith => 'Or continue with';

  @override
  String get agreeToTerms => 'I agree to the Terms & Conditions';

  @override
  String get alreadyHaveAccount => 'Already have an account?';

  @override
  String get dontHaveAccount => 'Don\'t have an account?';

  @override
  String get verificationCodeSent => 'Verification code sent';

  @override
  String get enterVerificationCode => 'Enter verification code';

  @override
  String get resendCode => 'Resend code';

  @override
  String get home => 'Home';

  @override
  String get workouts => 'Workouts';

  @override
  String get progress => 'Progress';

  @override
  String get schedule => 'Schedule';

  @override
  String get more => 'More';

  @override
  String get clients => 'Clients';

  @override
  String get programs => 'Programs';

  @override
  String get dashboard => 'Dashboard';

  @override
  String get chat => 'Chat';

  @override
  String get nutrition => 'Nutrition';

  @override
  String get billing => 'Billing';

  @override
  String get goodMorning => 'Good Morning';

  @override
  String get goodAfternoon => 'Good Afternoon';

  @override
  String get goodEvening => 'Good Evening';

  @override
  String greetingWithName(String name) {
    return 'Hello $name';
  }

  @override
  String get todaysWorkout => 'Today\'s Workout';

  @override
  String get nextSession => 'Next Session';

  @override
  String get streakDays => 'Streak Days';

  @override
  String streakCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count day streak',
      one: '1 day streak',
      zero: 'No streak',
    );
    return '$_temp0';
  }

  @override
  String get quickActions => 'Quick Actions';

  @override
  String get recentActivity => 'Recent Activity';

  @override
  String get startWorkout => 'Start Workout';

  @override
  String get viewAll => 'View All';

  @override
  String get noWorkoutToday => 'No workout scheduled today';

  @override
  String get upcomingClasses => 'Upcoming Classes';

  @override
  String get exerciseLibrary => 'Exercise Library';

  @override
  String get buildWorkout => 'Build Workout';

  @override
  String get workoutTemplates => 'Workout Templates';

  @override
  String get sets => 'Sets';

  @override
  String get reps => 'Reps';

  @override
  String get weight => 'Weight';

  @override
  String get rest => 'Rest';

  @override
  String get duration => 'Duration';

  @override
  String get distance => 'Distance';

  @override
  String get exerciseName => 'Exercise Name';

  @override
  String get muscleGroup => 'Muscle Group';

  @override
  String get chest => 'Chest';

  @override
  String get back => 'Back';

  @override
  String get shoulders => 'Shoulders';

  @override
  String get biceps => 'Biceps';

  @override
  String get triceps => 'Triceps';

  @override
  String get legs => 'Legs';

  @override
  String get abs => 'Abs';

  @override
  String get cardio => 'Cardio';

  @override
  String get fullBody => 'Full Body';

  @override
  String get workoutName => 'Workout Name';

  @override
  String get addExercise => 'Add Exercise';

  @override
  String get removeExercise => 'Remove Exercise';

  @override
  String get reorderExercises => 'Reorder Exercises';

  @override
  String get superSet => 'Super Set';

  @override
  String get dropSet => 'Drop Set';

  @override
  String get warmUp => 'Warm Up';

  @override
  String get coolDown => 'Cool Down';

  @override
  String get personalRecord => 'New Personal Record!';

  @override
  String get workoutComplete => 'Workout Complete!';

  @override
  String get totalVolume => 'Total Volume';

  @override
  String get workoutDuration => 'Workout Duration';

  @override
  String get assignWorkout => 'Assign Workout';

  @override
  String get assignProgram => 'Assign Program';

  @override
  String get programName => 'Program Name';

  @override
  String get programWeeks => 'Number of Weeks';

  @override
  String get daysPerWeek => 'Days per Week';

  @override
  String get remoteWorkout => 'Remote Workout';

  @override
  String get remoteWorkoutEnabled => 'Remote workout enabled';

  @override
  String get remoteWorkoutDisabled => 'Remote workout disabled';

  @override
  String get remoteReason => 'Activation reason';

  @override
  String get workoutHistory => 'Workout History';

  @override
  String get noWorkoutsYet => 'No workouts yet';

  @override
  String get bodyWeight => 'Body Weight';

  @override
  String get bodyFat => 'Body Fat %';

  @override
  String get measurements => 'Measurements';

  @override
  String get progressPhotos => 'Progress Photos';

  @override
  String get compare => 'Compare';

  @override
  String get addMeasurement => 'Add Measurement';

  @override
  String get waist => 'Waist';

  @override
  String get hips => 'Hips';

  @override
  String get chestMeasurement => 'Chest';

  @override
  String get armLeft => 'Left Arm';

  @override
  String get armRight => 'Right Arm';

  @override
  String get thighLeft => 'Left Thigh';

  @override
  String get thighRight => 'Right Thigh';

  @override
  String get takePhoto => 'Take Photo';

  @override
  String get chooseFromGallery => 'Choose from Gallery';

  @override
  String get front => 'Front';

  @override
  String get side => 'Side';

  @override
  String get backPose => 'Back';

  @override
  String get progressChart => 'Progress Chart';

  @override
  String get noProgressData => 'No progress data yet';

  @override
  String get goal => 'Goal';

  @override
  String get current => 'Current';

  @override
  String get change => 'Change';

  @override
  String get bookSession => 'Book Session';

  @override
  String get cancelSession => 'Cancel Session';

  @override
  String get cancelBooking => 'Cancel Booking';

  @override
  String get waitingList => 'Waiting List';

  @override
  String get groupClasses => 'Group Classes';

  @override
  String get availableSlots => 'Available Slots';

  @override
  String get fullyBooked => 'Fully Booked';

  @override
  String get joinWaitingList => 'Join Waiting List';

  @override
  String get sessionDetails => 'Session Details';

  @override
  String get sessionDate => 'Session Date';

  @override
  String get sessionTime => 'Session Time';

  @override
  String get sessionType => 'Session Type';

  @override
  String get privateSession => 'Private Session';

  @override
  String get groupSession => 'Group Session';

  @override
  String get confirmed => 'Confirmed';

  @override
  String get pending => 'Pending';

  @override
  String get cancelled => 'Cancelled';

  @override
  String get completed => 'Completed';

  @override
  String get noShow => 'No Show';

  @override
  String get lateCancel => 'Late Cancel';

  @override
  String get lateCancelWarning =>
      'A session will be deducted from your package for late cancellation';

  @override
  String cancelWindowHours(int hours) {
    return 'Must cancel at least $hours hours in advance';
  }

  @override
  String get trainerAvailability => 'Trainer Availability';

  @override
  String get setAvailability => 'Set Availability';

  @override
  String get noSessionsScheduled => 'No sessions scheduled';

  @override
  String get liveSessionMode => 'Live Session Mode';

  @override
  String get startSession => 'Start Session';

  @override
  String get endSession => 'End Session';

  @override
  String get sessionNotes => 'Session Notes';

  @override
  String get trainerNotes => 'Trainer Notes';

  @override
  String get foodLog => 'Food Log';

  @override
  String get waterTracking => 'Water Tracking';

  @override
  String get mealPlan => 'Meal Plan';

  @override
  String get addFood => 'Add Food';

  @override
  String get searchFood => 'Search food';

  @override
  String get calories => 'Calories';

  @override
  String get protein => 'Protein';

  @override
  String get carbs => 'Carbs';

  @override
  String get fat => 'Fat';

  @override
  String get fiber => 'Fiber';

  @override
  String get breakfast => 'Breakfast';

  @override
  String get lunch => 'Lunch';

  @override
  String get dinner => 'Dinner';

  @override
  String get snack => 'Snack';

  @override
  String get servingSize => 'Serving Size';

  @override
  String get gram => 'Gram';

  @override
  String get cup => 'Cup';

  @override
  String get piece => 'Piece';

  @override
  String get dailyGoal => 'Daily Goal';

  @override
  String waterCups(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count cups',
      one: '1 cup',
      zero: 'No water',
    );
    return '$_temp0';
  }

  @override
  String get addWater => 'Add Water';

  @override
  String get remaining => 'Remaining';

  @override
  String get consumed => 'Consumed';

  @override
  String get macros => 'Macros';

  @override
  String get noMealPlan => 'No meal plan';

  @override
  String get conversations => 'Conversations';

  @override
  String get typeMessage => 'Type a message...';

  @override
  String get sendMessage => 'Send';

  @override
  String get announcements => 'Announcements';

  @override
  String get notifications => 'Notifications';

  @override
  String get markAllRead => 'Mark all as read';

  @override
  String get noNotifications => 'No notifications';

  @override
  String get noConversations => 'No conversations';

  @override
  String get newAnnouncement => 'New Announcement';

  @override
  String get announcementTitle => 'Announcement Title';

  @override
  String get announcementBody => 'Announcement Body';

  @override
  String get notificationPreferences => 'Notification Preferences';

  @override
  String get pushNotifications => 'Push Notifications';

  @override
  String get emailNotifications => 'Email Notifications';

  @override
  String get recordPayment => 'Record Payment';

  @override
  String get packages => 'Packages';

  @override
  String get receipt => 'Receipt';

  @override
  String get cash => 'Cash';

  @override
  String get card => 'Card';

  @override
  String get bankTransfer => 'Bank Transfer';

  @override
  String get paymentAmount => 'Amount';

  @override
  String get paymentDate => 'Payment Date';

  @override
  String get paymentStatus => 'Payment Status';

  @override
  String get paid => 'Paid';

  @override
  String get voided => 'Voided';

  @override
  String get overdue => 'Overdue';

  @override
  String get membershipPlan => 'Membership Plan';

  @override
  String get renewMembership => 'Renew Membership';

  @override
  String get membershipExpiry => 'Membership Expiry';

  @override
  String get membershipExpired => 'Membership Expired';

  @override
  String get membershipActive => 'Membership Active';

  @override
  String get sessionPackage => 'Session Package';

  @override
  String get remainingSessions => 'Remaining Sessions';

  @override
  String sessionsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count sessions remaining',
      one: '1 session remaining',
      zero: 'No sessions remaining',
    );
    return '$_temp0';
  }

  @override
  String get viewReceipt => 'View Receipt';

  @override
  String get paymentHistory => 'Payment History';

  @override
  String get noPayments => 'No payments';

  @override
  String get egp => 'EGP';

  @override
  String get leaderboard => 'Leaderboard';

  @override
  String get trainerRating => 'Trainer Rating';

  @override
  String get trainerOfTheMonth => 'Trainer of the Month';

  @override
  String get certificates => 'Certificates';

  @override
  String get badges => 'Badges';

  @override
  String get points => 'Points';

  @override
  String get rank => 'Rank';

  @override
  String get rateTrainer => 'Rate Trainer';

  @override
  String get ratingSubmitted => 'Rating submitted';

  @override
  String get optOutLeaderboard => 'Opt out of leaderboard';

  @override
  String get optInLeaderboard => 'Opt into leaderboard';

  @override
  String get trainerProfile => 'Trainer Profile';

  @override
  String get monthlyScore => 'Monthly Score';

  @override
  String get noLeaderboardData => 'No leaderboard data';

  @override
  String get checkIn => 'Check In';

  @override
  String get memberNumber => 'Member Number';

  @override
  String get scanQR => 'Scan QR';

  @override
  String welcomeTo(String gymName) {
    return 'Welcome to $gymName';
  }

  @override
  String get membershipExpiredCheckIn => 'Your membership has expired';

  @override
  String get checkInSuccess => 'Check-in successful';

  @override
  String get checkInFailed => 'Check-in failed';

  @override
  String get kioskMode => 'Kiosk Mode';

  @override
  String get enterMemberNumber => 'Enter member number';

  @override
  String get numpadEntry => 'Numpad Entry';

  @override
  String get qrEntry => 'QR Scan';

  @override
  String get staff => 'Staff';

  @override
  String get owner => 'Owner';

  @override
  String get manager => 'Manager';

  @override
  String get receptionist => 'Receptionist';

  @override
  String get trainer => 'Trainer';

  @override
  String get member => 'Member';

  @override
  String get admin => 'Admin';

  @override
  String get addStaff => 'Add Staff';

  @override
  String get editStaff => 'Edit Staff';

  @override
  String get removeStaff => 'Remove Staff';

  @override
  String get staffRole => 'Staff Role';

  @override
  String get settings => 'Settings';

  @override
  String get language => 'Language';

  @override
  String get arabic => 'Arabic';

  @override
  String get english => 'English';

  @override
  String get units => 'Units';

  @override
  String get kg => 'Kilogram';

  @override
  String get lb => 'Pound';

  @override
  String get cm => 'Centimeter';

  @override
  String get inch => 'Inch';

  @override
  String get downloadMyData => 'Download My Data';

  @override
  String get deleteMyAccount => 'Delete My Account';

  @override
  String get darkMode => 'Dark Mode';

  @override
  String get lightMode => 'Light Mode';

  @override
  String get systemTheme => 'System Theme';

  @override
  String get appVersion => 'App Version';

  @override
  String get termsOfService => 'Terms of Service';

  @override
  String get privacyPolicy => 'Privacy Policy';

  @override
  String get aboutUs => 'About';

  @override
  String get contactSupport => 'Contact Support';

  @override
  String get profile => 'Profile';

  @override
  String get editProfile => 'Edit Profile';

  @override
  String get changePassword => 'Change Password';

  @override
  String get currentPassword => 'Current Password';

  @override
  String get newPassword => 'New Password';

  @override
  String get freePlan => 'Free Plan';

  @override
  String get starterPlan => 'Starter';

  @override
  String get proPlan => 'Pro';

  @override
  String get businessPlan => 'Business';

  @override
  String get upgrade => 'Upgrade';

  @override
  String get featureNotAvailable =>
      'This feature is not available on your current plan';

  @override
  String get upgradeToUnlock => 'Upgrade to unlock this feature';

  @override
  String get currentPlan => 'Current Plan';

  @override
  String get planFeatures => 'Plan Features';

  @override
  String get comparePlans => 'Compare Plans';

  @override
  String get storageLimitReached => 'Storage limit reached';

  @override
  String storageUsed(String used, String total) {
    return 'Storage used: $used of $total';
  }

  @override
  String get save => 'Save';

  @override
  String get cancel => 'Cancel';

  @override
  String get confirm => 'Confirm';

  @override
  String get delete => 'Delete';

  @override
  String get edit => 'Edit';

  @override
  String get add => 'Add';

  @override
  String get search => 'Search';

  @override
  String get filter => 'Filter';

  @override
  String get sort => 'Sort';

  @override
  String get loading => 'Loading...';

  @override
  String get error => 'Error';

  @override
  String get empty => 'Empty';

  @override
  String get retry => 'Retry';

  @override
  String get ok => 'OK';

  @override
  String get yes => 'Yes';

  @override
  String get no => 'No';

  @override
  String get close => 'Close';

  @override
  String get done => 'Done';

  @override
  String get next => 'Next';

  @override
  String get previous => 'Previous';

  @override
  String get submit => 'Submit';

  @override
  String get refresh => 'Refresh';

  @override
  String get share => 'Share';

  @override
  String get copy => 'Copy';

  @override
  String get selectAll => 'Select All';

  @override
  String get deselectAll => 'Deselect All';

  @override
  String get required => 'Required';

  @override
  String get optional => 'Optional';

  @override
  String get noResults => 'No results found';

  @override
  String get pullToRefresh => 'Pull to refresh';

  @override
  String get seeMore => 'See More';

  @override
  String get seeLess => 'See Less';

  @override
  String get comingSoon => 'Coming Soon';

  @override
  String get dataProcessingConsent => 'Data Processing Consent';

  @override
  String get dataProcessingConsentDescription =>
      'I consent to the processing of my personal data in accordance with the Egyptian Personal Data Protection Law';

  @override
  String get marketingConsent => 'Marketing Consent';

  @override
  String get marketingConsentDescription =>
      'I agree to receive marketing messages and special offers';

  @override
  String get downloadMyDataDescription =>
      'Download a copy of all your stored personal data';

  @override
  String get deleteMyAccountDescription =>
      'Permanently delete your account and all your data. This action cannot be undone.';

  @override
  String get areYouSure => 'Are you sure?';

  @override
  String get deleteAccountConfirmation =>
      'Your account and all data will be permanently deleted. Do you want to continue?';

  @override
  String get dataExportRequested =>
      'Data export requested. You will be notified when the file is ready.';

  @override
  String get consentUpdated => 'Consent updated';

  @override
  String get privacySettings => 'Privacy Settings';

  @override
  String dataRetentionNotice(int days) {
    return 'Your data will be retained for $days days after account deletion as required by law';
  }

  @override
  String get gym => 'Gym';

  @override
  String get members => 'Members';

  @override
  String membersCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count members',
      one: '1 member',
      zero: 'No members',
    );
    return '$_temp0';
  }

  @override
  String get trainers => 'Trainers';

  @override
  String trainersCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count trainers',
      one: '1 trainer',
      zero: 'No trainers',
    );
    return '$_temp0';
  }

  @override
  String get classes => 'Classes';

  @override
  String classesCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count classes',
      one: '1 class',
      zero: 'No classes',
    );
    return '$_temp0';
  }

  @override
  String get exercises => 'Exercises';

  @override
  String get invoices => 'Invoices';

  @override
  String get addMember => 'Add Member';

  @override
  String get editMember => 'Edit Member';

  @override
  String get memberDetails => 'Member Details';

  @override
  String memberSince(String date) {
    return 'Member since $date';
  }

  @override
  String get addTrainer => 'Add Trainer';

  @override
  String get addClass => 'Add Class';

  @override
  String get classCapacity => 'Class Capacity';

  @override
  String get className => 'Class Name';

  @override
  String get classSchedule => 'Class Schedule';

  @override
  String get gymInfo => 'Gym Info';

  @override
  String get gymName => 'Gym Name';

  @override
  String get gymAddress => 'Gym Address';

  @override
  String get operatingHours => 'Operating Hours';

  @override
  String get seasonalHours => 'Seasonal Hours';

  @override
  String get gymSections => 'Gym Sections';

  @override
  String get mensSection => 'Men\'s Section';

  @override
  String get womensSection => 'Women\'s Section';

  @override
  String get mixedSection => 'Mixed Section';

  @override
  String get switchGym => 'Switch Gym';

  @override
  String get selectGym => 'Select Gym';

  @override
  String get analytics => 'Analytics';

  @override
  String get reports => 'Reports';

  @override
  String get gymSuspended => 'Gym Subscription Suspended';

  @override
  String get gymSuspendedMessage =>
      'Please contact gym management for more information';

  @override
  String get contactGymAdmin => 'Contact Gym Admin';

  @override
  String get accountSuspended => 'Account Suspended';

  @override
  String get subscriptionExpired => 'Subscription Expired';

  @override
  String get somethingWentWrong => 'Something went wrong';

  @override
  String get memberNotFound => 'Member not found';

  @override
  String get sessionNotAvailable => 'Session not available';

  @override
  String get noInternetConnection => 'No internet connection';

  @override
  String get serverError => 'Server error';

  @override
  String get unauthorizedError =>
      'You are not authorized to perform this action';

  @override
  String get sessionExpired => 'Session expired. Please log in again.';

  @override
  String get invalidInput => 'Invalid input';

  @override
  String get emailAlreadyExists => 'Email already registered';

  @override
  String get weakPassword => 'Password is too weak';

  @override
  String get passwordMismatch => 'Passwords do not match';

  @override
  String get fieldRequired => 'This field is required';

  @override
  String get invalidEmail => 'Invalid email address';

  @override
  String get invalidPhone => 'Invalid phone number';

  @override
  String get fileTooLarge => 'File is too large';

  @override
  String get uploadFailed => 'Upload failed';

  @override
  String get timeoutError => 'Request timed out';

  @override
  String get trainerNotFound => 'Trainer not found';

  @override
  String get classFullError => 'Class is full';

  @override
  String get alreadyBooked => 'Already booked';

  @override
  String get cannotCancelPastSession => 'Cannot cancel a past session';

  @override
  String get today => 'Today';

  @override
  String get yesterday => 'Yesterday';

  @override
  String get tomorrow => 'Tomorrow';

  @override
  String get thisWeek => 'This Week';

  @override
  String get thisMonth => 'This Month';

  @override
  String get lastWeek => 'Last Week';

  @override
  String get lastMonth => 'Last Month';

  @override
  String minutesAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count minutes ago',
      one: '1 minute ago',
    );
    return '$_temp0';
  }

  @override
  String hoursAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count hours ago',
      one: '1 hour ago',
    );
    return '$_temp0';
  }

  @override
  String daysAgo(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count days ago',
      one: '1 day ago',
    );
    return '$_temp0';
  }

  @override
  String get justNow => 'Just now';

  @override
  String get date => 'Date';

  @override
  String get time => 'Time';

  @override
  String get from => 'From';

  @override
  String get to => 'To';

  @override
  String get am => 'AM';

  @override
  String get pm => 'PM';

  @override
  String get saturday => 'Saturday';

  @override
  String get sunday => 'Sunday';

  @override
  String get monday => 'Monday';

  @override
  String get tuesday => 'Tuesday';

  @override
  String get wednesday => 'Wednesday';

  @override
  String get thursday => 'Thursday';

  @override
  String get friday => 'Friday';

  @override
  String get membersLabel => 'member';

  @override
  String get sessionsLabel => 'session';

  @override
  String workoutsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count workouts',
      one: '1 workout',
      zero: 'No workouts',
    );
    return '$_temp0';
  }

  @override
  String daysCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count days',
      one: '1 day',
      zero: 'No days',
    );
    return '$_temp0';
  }

  @override
  String setsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count sets',
      one: '1 set',
      zero: 'No sets',
    );
    return '$_temp0';
  }

  @override
  String repsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count reps',
      one: '1 rep',
      zero: 'No reps',
    );
    return '$_temp0';
  }

  @override
  String notificationsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count notifications',
      one: '1 notification',
      zero: 'No notifications',
    );
    return '$_temp0';
  }

  @override
  String paymentsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count payments',
      one: '1 payment',
      zero: 'No payments',
    );
    return '$_temp0';
  }

  @override
  String photosCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count photos',
      one: '1 photo',
      zero: 'No photos',
    );
    return '$_temp0';
  }

  @override
  String get earnings => 'Earnings';

  @override
  String get totalEarnings => 'Total Earnings';

  @override
  String get thisMonthEarnings => 'This Month\'s Earnings';

  @override
  String get earningsReadOnly => 'Earnings are read-only';

  @override
  String get clientProfile => 'Client Profile';

  @override
  String get clientWorkouts => 'Client Workouts';

  @override
  String get clientProgress => 'Client Progress';

  @override
  String get clientNutrition => 'Client Nutrition';

  @override
  String get clientSchedule => 'Client Schedule';

  @override
  String get clientNotes => 'Client Notes';

  @override
  String get totalClients => 'Total Clients';

  @override
  String get activeClients => 'Active Clients';

  @override
  String get sessionsToday => 'Sessions Today';

  @override
  String get sessionsThisWeek => 'Sessions This Week';

  @override
  String get active => 'Active';

  @override
  String get inactive => 'Inactive';

  @override
  String get suspended => 'Suspended';

  @override
  String get expired => 'Expired';

  @override
  String get all => 'All';

  @override
  String get male => 'Male';

  @override
  String get female => 'Female';

  @override
  String get age => 'Age';

  @override
  String get height => 'Height';

  @override
  String get dateOfBirth => 'Date of Birth';

  @override
  String get gender => 'Gender';

  @override
  String get status => 'Status';

  @override
  String get notes => 'Notes';

  @override
  String get description => 'Description';

  @override
  String get title => 'Title';

  @override
  String get type => 'Type';

  @override
  String get category => 'Category';

  @override
  String get total => 'Total';

  @override
  String get average => 'Average';

  @override
  String get min => 'Min';

  @override
  String get max => 'Max';

  @override
  String get percentage => 'Percentage';

  @override
  String get offlineMode => 'Offline Mode';

  @override
  String get offlineChangesWillSync =>
      'Changes will sync when you\'re back online';

  @override
  String get syncing => 'Syncing...';

  @override
  String get syncComplete => 'Sync complete';

  @override
  String get syncFailed => 'Sync failed';

  @override
  String lastSynced(String time) {
    return 'Last synced: $time';
  }

  @override
  String get configUpdated => 'Settings updated';

  @override
  String get newFeatureAvailable => 'New feature available!';

  @override
  String get appUpdateAvailable => 'App update available';

  @override
  String get updateNow => 'Update Now';

  @override
  String get later => 'Later';

  @override
  String get successMessage => 'Operation successful';

  @override
  String get savedSuccessfully => 'Saved successfully';

  @override
  String get deletedSuccessfully => 'Deleted successfully';

  @override
  String get copied => 'Copied';

  @override
  String get sessionPolicies => 'Session Policies';

  @override
  String get cancellationWindow => 'Cancellation Window';

  @override
  String get noShowPolicy => 'No-Show Policy';

  @override
  String get lateCancelPolicy => 'Late Cancel Policy';

  @override
  String get deductFromPackage => 'Deduct from package';

  @override
  String get exportAsPdf => 'Export as PDF';

  @override
  String get print => 'Print';

  @override
  String get downloadReport => 'Download Report';

  @override
  String get dateRange => 'Date Range';

  @override
  String get startDate => 'Start Date';

  @override
  String get endDate => 'End Date';

  @override
  String get apply => 'Apply';

  @override
  String get reset => 'Reset';

  @override
  String get noData => 'No data available';

  @override
  String get totalMembers => 'Total Members';

  @override
  String get newMembers => 'New Members';

  @override
  String get activeMembers => 'Active Members';

  @override
  String get revenue => 'Revenue';

  @override
  String get attendance => 'Attendance';

  @override
  String get attendanceRate => 'Attendance Rate';

  @override
  String get checkInCount => 'Check-in Count';

  @override
  String get popularClasses => 'Popular Classes';

  @override
  String get peakHours => 'Peak Hours';
}
