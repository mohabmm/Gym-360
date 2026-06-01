import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart' show TelemetryNavigatorObserver;

import 'providers.dart';
import '../features/auth/presentation/screens/login_screen.dart';
import '../features/auth/presentation/screens/register_screen.dart';
import '../features/auth/presentation/screens/consent_screen.dart';
import '../features/auth/presentation/screens/profile_setup_screen.dart';
import '../features/auth/presentation/screens/link_gym_screen.dart';
import '../features/home/presentation/screens/home_screen.dart';
import '../features/workouts/presentation/screens/exercise_library_screen.dart';
import '../features/workouts/presentation/screens/guided_workout_screen.dart';
import '../features/workouts/presentation/screens/ad_hoc_workout_screen.dart';
import '../features/workouts/presentation/screens/log_workout_screen.dart';
import '../features/workouts/presentation/screens/workout_history_screen.dart';
import '../features/progress/presentation/screens/progress_dashboard_screen.dart';
import '../features/progress/presentation/screens/log_weight_screen.dart';
import '../features/progress/presentation/screens/log_measurements_screen.dart';
import '../features/progress/presentation/screens/progress_photos_screen.dart';
import '../features/scheduling/presentation/screens/upcoming_sessions_screen.dart';
import '../features/scheduling/presentation/screens/book_session_screen.dart';
import '../features/scheduling/presentation/screens/session_feedback_screen.dart';
import '../features/scheduling/presentation/screens/session_detail_screen.dart';
import '../features/scheduling/presentation/screens/class_schedule_screen.dart';
import '../features/nutrition/presentation/screens/nutrition_log_screen.dart';
import '../features/nutrition/presentation/screens/water_tracker_screen.dart';
import '../features/nutrition/presentation/screens/meal_plan_screen.dart';
import '../features/communication/presentation/screens/chat_screen.dart';
import '../features/communication/presentation/screens/notifications_screen.dart';
import '../features/communication/presentation/screens/announcements_screen.dart';
import '../features/gamification/presentation/screens/leaderboard_screen.dart';
import '../features/gamification/presentation/screens/trainer_public_profile_screen.dart';
import '../features/gamification/presentation/screens/rate_trainer_screen.dart';
import '../features/settings/presentation/screens/settings_screen.dart';
import '../features/checkin/presentation/screens/checkin_screen.dart';
import '../features/membership/presentation/screens/membership_screen.dart';
import 'shell_screen.dart';

// Route paths
class AppRoutes {
  AppRoutes._();

  static const String login = '/login';
  static const String register = '/register';
  static const String consent = '/consent';
  static const String profileSetup = '/profile-setup';
  static const String linkGym = '/link-gym';

  static const String home = '/';
  static const String workouts = '/workouts';
  static const String exerciseLibrary = '/workouts/exercises';
  static const String guidedWorkout = '/workouts/guided';
  static const String logWorkout = '/workouts/log';
  static const String workoutHistory = '/workouts/history';

  static const String progress = '/progress';
  static const String logWeight = '/progress/weight';
  static const String logMeasurements = '/progress/measurements';
  static const String progressPhotos = '/progress/photos';

  static const String schedule = '/schedule';
  static const String bookSession = '/schedule/book';
  static const String sessionFeedback = '/schedule/feedback/:sessionId';
  static const String sessionDetail = '/schedule/sessions/:sessionId';
  static const String classSchedule = '/schedule/classes';

  static const String more = '/more';
  static const String nutrition = '/more/nutrition';
  static const String waterTracker = '/more/nutrition/water';
  static const String mealPlan = '/more/nutrition/meal-plan';
  static const String chat = '/more/chat';
  static const String notifications = '/more/notifications';
  static const String announcements = '/more/announcements';
  static const String leaderboard = '/more/leaderboard';
  static const String trainerProfile = '/more/trainer/:trainerId';
  static const String rateTrainer = '/more/rate-trainer/:sessionId';
  static const String settings = '/more/settings';
  static const String checkin = '/more/checkin';
  static const String membership = '/more/membership';
}

final _rootNavigatorKey = GlobalKey<NavigatorState>();
final _shellNavigatorKey = GlobalKey<NavigatorState>();

/// Bridge so GoRouter re-evaluates its redirect whenever authProvider changes,
/// without recreating the entire GoRouter instance.
class _AuthChangeNotifier extends ChangeNotifier {
  _AuthChangeNotifier(Ref ref) {
    ref.listen(authProvider, (_, __) => notifyListeners());
  }
}

final routerProvider = Provider<GoRouter>((ref) {
  final authNotifier = _AuthChangeNotifier(ref);
  ref.onDispose(authNotifier.dispose);

  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: AppRoutes.home,
    observers: [TelemetryNavigatorObserver(ref.read(telemetryServiceProvider))],
    refreshListenable: authNotifier,
    redirect: (context, state) {
      final authState = ref.read(authProvider);
      final isLoading = authState.isLoading;
      final auth = authState.valueOrNull;

      final isAuthRoute = state.matchedLocation == AppRoutes.login ||
          state.matchedLocation == AppRoutes.register ||
          state.matchedLocation == AppRoutes.consent ||
          state.matchedLocation == AppRoutes.profileSetup ||
          state.matchedLocation == AppRoutes.linkGym;

      // While auth is loading (checking token/fetching profile), redirect
      // non-auth routes to login so the user never sees the home screen
      // before authentication is confirmed.
      if (isLoading || auth == null || auth.status == AuthStatus.unknown) {
        return isAuthRoute ? null : AppRoutes.login;
      }

      if (auth.status == AuthStatus.unauthenticated && !isAuthRoute) {
        return AppRoutes.login;
      }

      if (auth.status == AuthStatus.authenticated && isAuthRoute) {
        return AppRoutes.home;
      }

      return null;
    },
    routes: [
      // Auth routes (no shell)
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.register,
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: AppRoutes.consent,
        builder: (context, state) => const ConsentScreen(),
      ),
      GoRoute(
        path: AppRoutes.profileSetup,
        builder: (context, state) => const ProfileSetupScreen(),
      ),
      GoRoute(
        path: AppRoutes.linkGym,
        builder: (context, state) => const LinkGymScreen(),
      ),

      // Main app routes (with bottom nav shell)
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) => ShellScreen(child: child),
        routes: [
          // Tab 1: Home
          GoRoute(
            path: AppRoutes.home,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: HomeScreen(),
            ),
          ),

          // Tab 2: Workouts
          GoRoute(
            path: AppRoutes.workouts,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: ExerciseLibraryScreen(),
            ),
            routes: [
              GoRoute(
                path: 'exercises',
                builder: (context, state) => const ExerciseLibraryScreen(),
              ),
              GoRoute(
                path: 'guided',
                builder: (context, state) => GuidedWorkoutScreen(
                  workoutId: state.uri.queryParameters['workoutId'],
                ),
              ),
              GoRoute(
                path: 'log',
                builder: (context, state) => const LogWorkoutScreen(),
              ),
              GoRoute(
                path: 'ad-hoc',
                builder: (context, state) => const AdHocWorkoutScreen(),
              ),
              GoRoute(
                path: 'history',
                builder: (context, state) => const WorkoutHistoryScreen(),
              ),
            ],
          ),

          // Tab 3: Progress
          GoRoute(
            path: AppRoutes.progress,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: ProgressDashboardScreen(),
            ),
            routes: [
              GoRoute(
                path: 'weight',
                builder: (context, state) => const LogWeightScreen(),
              ),
              GoRoute(
                path: 'measurements',
                builder: (context, state) => const LogMeasurementsScreen(),
              ),
              GoRoute(
                path: 'photos',
                builder: (context, state) => const ProgressPhotosScreen(),
              ),
            ],
          ),

          // Tab 4: Schedule
          GoRoute(
            path: AppRoutes.schedule,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: UpcomingSessionsScreen(),
            ),
            routes: [
              GoRoute(
                path: 'book',
                builder: (context, state) => const BookSessionScreen(),
              ),
              GoRoute(
                path: 'feedback/:sessionId',
                builder: (context, state) => SessionFeedbackScreen(
                  sessionId: state.pathParameters['sessionId']!,
                ),
              ),
              GoRoute(
                path: 'sessions/:sessionId',
                builder: (context, state) => SessionDetailScreen(
                  sessionId: state.pathParameters['sessionId']!,
                ),
              ),
              GoRoute(
                path: 'classes',
                builder: (context, state) => const ClassScheduleScreen(),
              ),
            ],
          ),

          // Tab 5: More
          GoRoute(
            path: AppRoutes.more,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: _MoreScreen(),
            ),
            routes: [
              GoRoute(
                path: 'nutrition',
                builder: (context, state) => const NutritionLogScreen(),
                routes: [
                  GoRoute(
                    path: 'water',
                    builder: (context, state) => const WaterTrackerScreen(),
                  ),
                  GoRoute(
                    path: 'meal-plan',
                    builder: (context, state) => const MealPlanScreen(),
                  ),
                ],
              ),
              GoRoute(
                path: 'chat',
                builder: (context, state) => const ChatScreen(),
              ),
              GoRoute(
                path: 'notifications',
                builder: (context, state) => const NotificationsScreen(),
              ),
              GoRoute(
                path: 'announcements',
                builder: (context, state) => const AnnouncementsScreen(),
              ),
              GoRoute(
                path: 'leaderboard',
                builder: (context, state) => const LeaderboardScreen(),
              ),
              GoRoute(
                path: 'trainer/:trainerId',
                builder: (context, state) => TrainerPublicProfileScreen(
                  trainerId: state.pathParameters['trainerId']!,
                ),
              ),
              GoRoute(
                path: 'rate-trainer/:sessionId',
                builder: (context, state) => RateTrainerScreen(
                  sessionId: state.pathParameters['sessionId']!,
                ),
              ),
              GoRoute(
                path: 'settings',
                builder: (context, state) => const SettingsScreen(),
              ),
              GoRoute(
                path: 'checkin',
                builder: (context, state) => const CheckInScreen(),
              ),
              GoRoute(
                path: 'membership',
                builder: (context, state) => const MembershipScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});

/// Placeholder for the More tab - it's defined inline here since it's
/// a navigation hub rather than a feature screen.
class _MoreScreen extends ConsumerWidget {
  const _MoreScreen();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المزيد' : 'More'),
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          // User card
          if (user != null)
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundImage: user.photoUrl != null
                        ? NetworkImage(user.photoUrl!)
                        : null,
                    child: user.photoUrl == null
                        ? Text(
                            user.firstName[0].toUpperCase(),
                            style: const TextStyle(fontSize: 20),
                          )
                        : null,
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${user.firstName} ${user.lastName}',
                          style: theme.textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                        if (user.memberNumber != null)
                          Text(
                            '${isArabic ? 'رقم العضوية: ' : 'Member #'}${user.memberNumber}',
                            style: theme.textTheme.bodySmall,
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          const Divider(),
          _MoreTile(
            icon: Icons.qr_code,
            label: isArabic ? 'تسجيل الحضور' : 'Check-in',
            onTap: () => context.go(AppRoutes.checkin),
          ),
          _MoreTile(
            icon: Icons.card_membership,
            label: isArabic ? 'العضوية' : 'Membership',
            onTap: () => context.go(AppRoutes.membership),
          ),
          _MoreTile(
            icon: Icons.restaurant_outlined,
            label: isArabic ? 'التغذية' : 'Nutrition',
            onTap: () => context.go(AppRoutes.nutrition),
          ),
          _MoreTile(
            icon: Icons.chat_outlined,
            label: isArabic ? 'المحادثة' : 'Chat',
            onTap: () => context.go(AppRoutes.chat),
          ),
          _MoreTile(
            icon: Icons.campaign_outlined,
            label: isArabic ? 'الإعلانات' : 'Announcements',
            onTap: () => context.go(AppRoutes.announcements),
          ),
          _MoreTile(
            icon: Icons.leaderboard_outlined,
            label: isArabic ? 'المتصدرين' : 'Leaderboard',
            onTap: () => context.go(AppRoutes.leaderboard),
          ),
          const Divider(),
          _MoreTile(
            icon: Icons.settings_outlined,
            label: isArabic ? 'الإعدادات' : 'Settings',
            onTap: () => context.go(AppRoutes.settings),
          ),
        ],
      ),
    );
  }
}

class _MoreTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final Widget? trailing;

  const _MoreTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon),
      title: Text(label),
      trailing: trailing ?? const Icon(Icons.chevron_right, size: 20),
      onTap: onTap,
    );
  }
}
