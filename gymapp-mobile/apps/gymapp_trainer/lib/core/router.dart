import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart' show TelemetryNavigatorObserver;

import 'providers.dart';
import '../features/auth/presentation/screens/login_screen.dart';
import '../features/auth/presentation/screens/register_screen.dart';
import '../features/auth/presentation/screens/pending_approval_screen.dart';
import '../features/home/presentation/screens/trainer_home_screen.dart';
import '../features/clients/presentation/screens/client_roster_screen.dart';
import '../features/clients/presentation/screens/client_detail_screen.dart';
import '../features/clients/presentation/screens/onboarding_form_screen.dart';
import '../features/clients/presentation/screens/client_notes_screen.dart';
import '../features/clients/presentation/screens/assign_workout_screen.dart';
import '../features/workouts/presentation/screens/workout_builder_screen.dart';
import '../features/workouts/presentation/screens/template_library_screen.dart';
import '../features/workouts/presentation/screens/program_builder_screen.dart';
import '../features/workouts/presentation/screens/exercise_manager_screen.dart';
import '../features/scheduling/presentation/screens/availability_screen.dart';
import '../features/scheduling/presentation/screens/booking_requests_screen.dart';
import '../features/scheduling/presentation/screens/calendar_screen.dart';
import '../features/scheduling/presentation/screens/recurring_session_screen.dart';
import '../features/scheduling/presentation/screens/leave_management_screen.dart';
import '../features/sessions/presentation/screens/live_session_screen.dart';
import '../features/sessions/presentation/screens/post_session_log_screen.dart';
import '../features/communication/presentation/screens/chat_list_screen.dart';
import '../features/communication/presentation/screens/chat_screen.dart';
import '../features/communication/presentation/screens/notifications_screen.dart';
import '../features/communication/presentation/screens/broadcast_screen.dart';
import '../features/communication/presentation/screens/form_check_screen.dart';
import '../features/nutrition/presentation/screens/meal_plan_builder_screen.dart';
import '../features/nutrition/presentation/screens/client_nutrition_view_screen.dart';
import '../features/revenue/presentation/screens/client_packages_screen.dart';
import '../features/revenue/presentation/screens/earnings_dashboard_screen.dart';
import '../features/gamification/presentation/screens/my_ratings_dashboard_screen.dart';
import '../features/settings/presentation/screens/settings_screen.dart';
import '../features/settings/presentation/screens/profile_screen.dart';
import 'shell_screen.dart';

/// Route names for type-safe navigation.
class AppRoutes {
  AppRoutes._();

  static const String login = '/login';
  static const String register = '/register';
  static const String pendingApproval = '/pending-approval';
  static const String home = '/home';
  static const String clients = '/clients';
  static const String clientDetail = '/clients/:clientId';
  static const String clientOnboarding = '/clients/:clientId/onboarding';
  static const String clientNotes = '/clients/:clientId/notes';
  static const String assignWorkout = '/clients/:clientId/assign-workout';
  static const String workouts = '/workouts';
  static const String workoutBuilder = '/workouts/builder';
  static const String workoutBuilderEdit = '/workouts/builder/:workoutId';
  static const String templateLibrary = '/workouts/templates';
  static const String programBuilder = '/workouts/program-builder';
  static const String exerciseManager = '/workouts/exercises';
  static const String schedule = '/schedule';
  static const String availability = '/schedule/availability';
  static const String bookingRequests = '/schedule/booking-requests';
  static const String recurringSession = '/schedule/recurring';
  static const String leaveManagement = '/schedule/leave';
  static const String liveSession = '/sessions/:sessionId/live';
  static const String postSessionLog = '/sessions/:sessionId/log';
  static const String more = '/more';
  static const String chatList = '/more/chat';
  static const String chat = '/more/chat/:conversationId';
  static const String notifications = '/more/notifications';
  static const String broadcast = '/more/broadcast';
  static const String formCheck = '/more/form-check';
  static const String mealPlanBuilder = '/more/nutrition/meal-plan-builder';
  static const String clientNutrition = '/more/nutrition/client/:clientId';
  static const String clientPackages = '/more/packages';
  static const String earningsDashboard = '/more/earnings';
  static const String myRatings = '/more/ratings';
  static const String settings = '/more/settings';
  static const String profile = '/more/profile';
}

final _rootNavigatorKey = GlobalKey<NavigatorState>();
final _shellNavigatorKey = GlobalKey<NavigatorState>();

/// Tracks whether the router has already performed a first-redirect check.
/// On cold start we force the user to a safe root (Home or the auth gate),
/// so a previously-crashed detail route (e.g. a 405-ing /clients/:id page)
/// can't be re-entered just because the OS tried to restore the last URL.
bool _coldStartRedirected = false;

final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: AppRoutes.home,
    debugLogDiagnostics: true,
    observers: [TelemetryNavigatorObserver(ref.read(telemetryServiceProvider))],
    redirect: (context, state) {
      final authState = ref.read(authProvider);
      final isAuth = authState.status == AuthStatus.authenticated;
      final isPending = authState.status == AuthStatus.pending;
      final isInitial = authState.status == AuthStatus.initial;
      final isLoginRoute = state.matchedLocation == AppRoutes.login;
      final isRegisterRoute = state.matchedLocation == AppRoutes.register;
      final isPendingRoute =
          state.matchedLocation == AppRoutes.pendingApproval;

      // Startup: token is being validated via /profile. Don't kick the user
      // to /login before the check resolves — if it succeeds they stay on
      // /home, if it fails the next redirect pass will send them to /login.
      if (isInitial) {
        return null;
      }

      if (isPending && !isPendingRoute) {
        return AppRoutes.pendingApproval;
      }

      if (!isAuth && !isPending && !isLoginRoute && !isRegisterRoute) {
        return AppRoutes.login;
      }

      if (isAuth && (isLoginRoute || isRegisterRoute || isPendingRoute)) {
        return AppRoutes.home;
      }

      // Once auth is resolved, force the very first post-auth navigation to
      // the Home tab. Otherwise a restored deep-link (e.g. a detail route
      // that previously 405'd) would re-crash the app on relaunch.
      if (isAuth && !_coldStartRedirected) {
        _coldStartRedirected = true;
        if (state.matchedLocation != AppRoutes.home) {
          return AppRoutes.home;
        }
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
        path: AppRoutes.pendingApproval,
        builder: (context, state) => const PendingApprovalScreen(),
      ),

      // Main shell with bottom navigation
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) => ShellScreen(child: child),
        routes: [
          // Home tab
          GoRoute(
            path: AppRoutes.home,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: TrainerHomeScreen(),
            ),
          ),

          // Clients tab
          GoRoute(
            path: AppRoutes.clients,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: ClientRosterScreen(),
            ),
          ),

          // Workouts tab
          GoRoute(
            path: AppRoutes.workouts,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: TemplateLibraryScreen(),
            ),
          ),

          // Schedule tab
          GoRoute(
            path: AppRoutes.schedule,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: CalendarScreen(),
            ),
          ),

          // More tab
          GoRoute(
            path: AppRoutes.more,
            pageBuilder: (context, state) => const NoTransitionPage(
              child: MoreMenuScreen(),
            ),
          ),
        ],
      ),

      // Detail routes (outside shell, full screen)
      GoRoute(
        path: AppRoutes.clientDetail,
        builder: (context, state) {
          final clientId = state.pathParameters['clientId']!;
          return ClientDetailScreen(clientId: clientId);
        },
      ),
      GoRoute(
        path: AppRoutes.clientOnboarding,
        builder: (context, state) {
          final clientId = state.pathParameters['clientId']!;
          return OnboardingFormScreen(clientId: clientId);
        },
      ),
      GoRoute(
        path: AppRoutes.clientNotes,
        builder: (context, state) {
          final clientId = state.pathParameters['clientId']!;
          return ClientNotesScreen(clientId: clientId);
        },
      ),
      GoRoute(
        path: AppRoutes.assignWorkout,
        builder: (context, state) {
          final clientId = state.pathParameters['clientId']!;
          return AssignWorkoutScreen(clientId: clientId);
        },
      ),
      GoRoute(
        path: AppRoutes.workoutBuilder,
        builder: (context, state) => const WorkoutBuilderScreen(),
      ),
      GoRoute(
        path: AppRoutes.workoutBuilderEdit,
        builder: (context, state) {
          final workoutId = state.pathParameters['workoutId'];
          return WorkoutBuilderScreen(workoutId: workoutId);
        },
      ),
      GoRoute(
        path: AppRoutes.templateLibrary,
        builder: (context, state) => const TemplateLibraryScreen(),
      ),
      GoRoute(
        path: AppRoutes.programBuilder,
        builder: (context, state) => const ProgramBuilderScreen(),
      ),
      GoRoute(
        path: AppRoutes.exerciseManager,
        builder: (context, state) => const ExerciseManagerScreen(),
      ),
      GoRoute(
        path: AppRoutes.availability,
        builder: (context, state) => const AvailabilityScreen(),
      ),
      GoRoute(
        path: AppRoutes.bookingRequests,
        builder: (context, state) => const BookingRequestsScreen(),
      ),
      GoRoute(
        path: AppRoutes.recurringSession,
        builder: (context, state) => const RecurringSessionScreen(),
      ),
      GoRoute(
        path: AppRoutes.leaveManagement,
        builder: (context, state) => const LeaveManagementScreen(),
      ),
      GoRoute(
        path: AppRoutes.liveSession,
        builder: (context, state) {
          final sessionId = state.pathParameters['sessionId']!;
          return LiveSessionScreen(sessionId: sessionId);
        },
      ),
      GoRoute(
        path: AppRoutes.postSessionLog,
        builder: (context, state) {
          final sessionId = state.pathParameters['sessionId']!;
          final initialNotes = state.extra is String ? state.extra as String : null;
          return PostSessionLogScreen(
            sessionId: sessionId,
            initialNotes: initialNotes,
          );
        },
      ),
      GoRoute(
        path: AppRoutes.chatList,
        builder: (context, state) => const ChatListScreen(),
      ),
      GoRoute(
        path: AppRoutes.chat,
        builder: (context, state) {
          final conversationId = state.pathParameters['conversationId']!;
          return ChatScreen(conversationId: conversationId);
        },
      ),
      GoRoute(
        path: AppRoutes.notifications,
        builder: (context, state) => const NotificationsScreen(),
      ),
      GoRoute(
        path: AppRoutes.broadcast,
        builder: (context, state) => const BroadcastScreen(),
      ),
      GoRoute(
        path: AppRoutes.formCheck,
        builder: (context, state) => const FormCheckScreen(),
      ),
      GoRoute(
        path: AppRoutes.mealPlanBuilder,
        builder: (context, state) => const MealPlanBuilderScreen(),
      ),
      GoRoute(
        path: AppRoutes.clientNutrition,
        builder: (context, state) {
          final clientId = state.pathParameters['clientId']!;
          return ClientNutritionViewScreen(clientId: clientId);
        },
      ),
      GoRoute(
        path: AppRoutes.clientPackages,
        builder: (context, state) => const ClientPackagesScreen(),
      ),
      GoRoute(
        path: AppRoutes.earningsDashboard,
        builder: (context, state) => const EarningsDashboardScreen(),
      ),
      GoRoute(
        path: AppRoutes.myRatings,
        builder: (context, state) => const MyRatingsDashboardScreen(),
      ),
      GoRoute(
        path: AppRoutes.settings,
        builder: (context, state) => const SettingsScreen(),
      ),
      GoRoute(
        path: AppRoutes.profile,
        builder: (context, state) => const ProfileScreen(),
      ),
    ],
  );

  // Call router.refresh() directly when auth state changes — more reliable
  // than refreshListenable which can miss state transitions in some Riverpod
  // versions.
  ref.listen<AuthState>(authProvider, (_, __) => router.refresh());

  return router;
});

/// The "More" tab menu screen.
class MoreMenuScreen extends ConsumerWidget {
  const MoreMenuScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المزيد' : 'More'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
        children: [
          _MoreMenuItem(
            icon: Icons.monetization_on_outlined,
            title: isArabic ? 'الارباح' : 'Earnings',
            subtitle: isArabic ? 'عرض الارباح والجلسات' : 'View earnings & sessions',
            onTap: () => context.push(AppRoutes.earningsDashboard),
          ),
          _MoreMenuItem(
            icon: Icons.chat_outlined,
            title: isArabic ? 'المحادثات' : 'Chat',
            subtitle: isArabic ? 'محادثات العملاء' : 'Client conversations',
            onTap: () => context.push(AppRoutes.chatList),
          ),
          _MoreMenuItem(
            icon: Icons.inventory_2_outlined,
            title: isArabic ? 'الباقات' : 'Packages',
            subtitle: isArabic ? 'عرض باقات العملاء' : 'View client packages',
            onTap: () => context.push(AppRoutes.clientPackages),
          ),
          _MoreMenuItem(
            icon: Icons.star_outline,
            title: isArabic ? 'تقييمي' : 'My Ratings',
            subtitle: isArabic ? 'التقييمات والشارات' : 'Ratings & badges',
            onTap: () => context.push(AppRoutes.myRatings),
          ),
          _MoreMenuItem(
            icon: Icons.restaurant_outlined,
            title: isArabic ? 'خطط التغذية' : 'Meal Plans',
            subtitle: isArabic ? 'بناء خطط تغذية للعملاء' : 'Build client meal plans',
            onTap: () => context.push(AppRoutes.mealPlanBuilder),
          ),
          _MoreMenuItem(
            icon: Icons.campaign_outlined,
            title: isArabic ? 'رسالة جماعية' : 'Broadcast',
            subtitle: isArabic ? 'ارسال رسالة لجميع العملاء' : 'Message all clients',
            onTap: () => context.push(AppRoutes.broadcast),
          ),
          _MoreMenuItem(
            icon: Icons.fitness_center_outlined,
            title: isArabic ? 'تفقد الشكل' : 'Form Check',
            subtitle: isArabic ? 'مراجعة فيديوهات التمرين' : 'Review exercise videos',
            onTap: () => context.push(AppRoutes.formCheck),
          ),
          const Divider(height: 32),
          _MoreMenuItem(
            icon: Icons.person_outline,
            title: isArabic ? 'الملف الشخصي' : 'Profile',
            subtitle: isArabic ? 'تعديل بياناتك' : 'Edit your info',
            onTap: () => context.push(AppRoutes.profile),
          ),
          _MoreMenuItem(
            icon: Icons.settings_outlined,
            title: isArabic ? 'الاعدادات' : 'Settings',
            subtitle: isArabic ? 'اللغة والاشعارات' : 'Language & notifications',
            onTap: () => context.push(AppRoutes.settings),
          ),
        ],
      ),
    );
  }
}

class _MoreMenuItem extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _MoreMenuItem({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      leading: Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          color: theme.colorScheme.primary.withOpacity(0.1),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(icon, color: theme.colorScheme.primary),
      ),
      title: Text(title, style: theme.textTheme.titleSmall),
      subtitle: Text(subtitle, style: theme.textTheme.bodySmall),
      trailing: const Icon(Icons.chevron_right),
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
    );
  }
}
