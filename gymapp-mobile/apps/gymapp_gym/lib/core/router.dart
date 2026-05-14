import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../features/announcements/presentation/screens/announcement_list_screen.dart';
import '../features/announcements/presentation/screens/create_announcement_screen.dart';
import '../features/auth/presentation/screens/login_screen.dart';
import '../features/billing/presentation/screens/active_packages_screen.dart';
import '../features/billing/presentation/screens/assign_package_screen.dart';
import '../features/billing/presentation/screens/package_templates_screen.dart';
import '../features/billing/presentation/screens/record_payment_screen.dart';
import '../features/billing/presentation/screens/revenue_dashboard_screen.dart';
import '../features/checkin/presentation/screens/check_in_kiosk_screen.dart';
import '../features/checkin/presentation/screens/check_in_log_screen.dart';
import '../features/checkin/presentation/screens/manual_check_in_screen.dart';
import '../features/classes/presentation/screens/class_detail_screen.dart';
import '../features/classes/presentation/screens/class_list_screen.dart';
import '../features/classes/presentation/screens/create_class_screen.dart';
import '../features/dashboard/presentation/screens/gym_dashboard_screen.dart';
import '../features/exercises/presentation/screens/custom_exercise_screen.dart';
import '../features/exercises/presentation/screens/exercise_library_screen.dart';
import '../features/gamification/presentation/screens/coach_leaderboard_screen.dart';
import '../features/gamification/presentation/screens/member_leaderboard_screen.dart';
import '../features/gamification/presentation/screens/moderate_testimonials_screen.dart';
import '../features/gamification/presentation/screens/trainer_ratings_detail_screen.dart';
import '../features/members/presentation/screens/add_member_screen.dart';
import '../features/members/presentation/screens/edit_member_screen.dart';
import '../features/members/presentation/screens/member_detail_screen.dart';
import '../features/members/presentation/screens/member_list_screen.dart';
import '../features/settings/presentation/screens/amenities_screen.dart';
import '../features/settings/presentation/screens/branding_screen.dart';
import '../features/settings/presentation/screens/gym_profile_screen.dart';
import '../features/settings/presentation/screens/gym_sections_screen.dart';
import '../features/settings/presentation/screens/kiosk_pin_screen.dart';
import '../features/settings/presentation/screens/testing_mode_screen.dart';
import '../features/settings/presentation/screens/notification_settings_screen.dart';
import '../features/settings/presentation/screens/seasonal_hours_screen.dart';
import '../features/settings/presentation/screens/session_policies_screen.dart';
import '../features/staff/presentation/screens/add_staff_screen.dart';
import '../features/staff/presentation/screens/permission_matrix_screen.dart';
import '../features/staff/presentation/screens/staff_list_screen.dart';
import '../features/trainers/presentation/screens/pending_trainer_approval_screen.dart';
import '../features/trainers/presentation/screens/trainer_detail_screen.dart';
import '../features/trainers/presentation/screens/trainer_list_screen.dart';
import 'permissions.dart';
import 'providers.dart';

// Route paths
abstract class RoutePaths {
  static const String login = '/login';
  static const String dashboard = '/';
  static const String members = '/members';
  static const String memberDetail = '/members/:id';
  static const String addMember = '/members/add';
  static const String editMember = '/members/:id/edit';
  static const String trainers = '/trainers';
  static const String trainerDetail = '/trainers/:id';
  static const String pendingTrainers = '/trainers/pending';
  static const String classes = '/classes';
  static const String classDetail = '/classes/:id';
  static const String createClass = '/classes/create';
  static const String exercises = '/exercises';
  static const String customExercises = '/exercises/custom';
  static const String kiosk = '/kiosk';
  static const String manualCheckIn = '/checkin/manual';
  static const String checkInLog = '/checkin/log';
  static const String recordPayment = '/billing/record';
  static const String packageTemplates = '/billing/templates';
  static const String assignPackage = '/billing/assign';
  static const String activePackages = '/billing/packages';
  static const String revenueDashboard = '/billing/revenue';
  static const String announcements = '/announcements';
  static const String createAnnouncement = '/announcements/create';
  static const String coachLeaderboard = '/gamification/coaches';
  static const String trainerRatings = '/gamification/ratings/:id';
  static const String moderateTestimonials = '/gamification/testimonials';
  static const String memberLeaderboard = '/gamification/members';
  static const String staff = '/staff';
  static const String addStaff = '/staff/add';
  static const String permissionMatrix = '/staff/permissions';
  static const String gymProfile = '/settings/profile';
  static const String branding = '/settings/branding';
  static const String amenities = '/settings/amenities';
  static const String notificationSettings = '/settings/notifications';
  static const String seasonalHours = '/settings/seasonal-hours';
  static const String gymSections = '/settings/sections';
  static const String sessionPolicies = '/settings/policies';
  static const String kioskPin = '/settings/kiosk-pin';
  static const String testingMode = '/settings/testing-mode';
}

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    initialLocation: RoutePaths.dashboard,
    debugLogDiagnostics: true,
    redirect: (context, state) {
      final auth = authState.valueOrNull;
      final isLoggedIn = auth?.status == AuthStatus.authenticated;
      final isLoginPage = state.matchedLocation == RoutePaths.login;
      final isKioskPage = state.matchedLocation == RoutePaths.kiosk;

      // Allow kiosk if authenticated
      if (isKioskPage && isLoggedIn) return null;

      if (!isLoggedIn && !isLoginPage) return RoutePaths.login;
      if (isLoggedIn && isLoginPage) return RoutePaths.dashboard;
      return null;
    },
    routes: [
      // Login
      GoRoute(
        path: RoutePaths.login,
        builder: (context, state) => const LoginScreen(),
      ),

      // Main shell with bottom navigation
      ShellRoute(
        builder: (context, state, child) => MainShell(child: child),
        routes: [
          // Dashboard
          GoRoute(
            path: RoutePaths.dashboard,
            builder: (context, state) => const GymDashboardScreen(),
          ),

          // Members
          GoRoute(
            path: RoutePaths.members,
            builder: (context, state) => const MemberListScreen(),
          ),
          GoRoute(
            path: RoutePaths.addMember,
            builder: (context, state) => const AddMemberScreen(),
          ),
          GoRoute(
            path: RoutePaths.memberDetail,
            builder: (context, state) => MemberDetailScreen(
              memberId: state.pathParameters['id']!,
            ),
          ),
          GoRoute(
            path: RoutePaths.editMember,
            builder: (context, state) => EditMemberScreen(
              memberId: state.pathParameters['id']!,
            ),
          ),

          // Trainers
          GoRoute(
            path: RoutePaths.trainers,
            builder: (context, state) => const TrainerListScreen(),
          ),
          // `/trainers/pending` must come before `/trainers/:id`, otherwise
          // go_router matches the dynamic route first and treats "pending"
          // as an id, routing to the trainer-detail screen.
          GoRoute(
            path: RoutePaths.pendingTrainers,
            builder: (context, state) =>
                const PendingTrainerApprovalScreen(),
          ),
          GoRoute(
            path: RoutePaths.trainerDetail,
            builder: (context, state) => TrainerDetailScreen(
              trainerId: state.pathParameters['id']!,
            ),
          ),

          // Classes
          GoRoute(
            path: RoutePaths.classes,
            builder: (context, state) => const ClassListScreen(),
          ),
          GoRoute(
            path: RoutePaths.createClass,
            builder: (context, state) => const CreateClassScreen(),
          ),
          GoRoute(
            path: RoutePaths.classDetail,
            builder: (context, state) => ClassDetailScreen(
              classId: state.pathParameters['id']!,
            ),
          ),

          // Exercises
          GoRoute(
            path: RoutePaths.exercises,
            builder: (context, state) => const ExerciseLibraryScreen(),
          ),
          GoRoute(
            path: RoutePaths.customExercises,
            builder: (context, state) => const CustomExerciseScreen(),
          ),

          // Check-in
          GoRoute(
            path: RoutePaths.manualCheckIn,
            builder: (context, state) => const ManualCheckInScreen(),
          ),
          GoRoute(
            path: RoutePaths.checkInLog,
            builder: (context, state) => const CheckInLogScreen(),
          ),

          // Billing
          GoRoute(
            path: RoutePaths.recordPayment,
            builder: (context, state) => const RecordPaymentScreen(),
          ),
          GoRoute(
            path: RoutePaths.packageTemplates,
            builder: (context, state) => const PackageTemplatesScreen(),
          ),
          GoRoute(
            path: RoutePaths.assignPackage,
            builder: (context, state) => const AssignPackageScreen(),
          ),
          GoRoute(
            path: RoutePaths.activePackages,
            builder: (context, state) => const ActivePackagesScreen(),
          ),
          GoRoute(
            path: RoutePaths.revenueDashboard,
            builder: (context, state) => const RevenueDashboardScreen(),
          ),

          // Announcements
          GoRoute(
            path: RoutePaths.announcements,
            builder: (context, state) => const AnnouncementListScreen(),
          ),
          GoRoute(
            path: RoutePaths.createAnnouncement,
            builder: (context, state) => const CreateAnnouncementScreen(),
          ),

          // Gamification
          GoRoute(
            path: RoutePaths.coachLeaderboard,
            builder: (context, state) => const CoachLeaderboardScreen(),
          ),
          GoRoute(
            path: RoutePaths.trainerRatings,
            builder: (context, state) => TrainerRatingsDetailScreen(
              trainerId: state.pathParameters['id']!,
            ),
          ),
          GoRoute(
            path: RoutePaths.moderateTestimonials,
            builder: (context, state) =>
                const ModerateTestimonialsScreen(),
          ),
          GoRoute(
            path: RoutePaths.memberLeaderboard,
            builder: (context, state) => const MemberLeaderboardScreen(),
          ),

          // Staff
          GoRoute(
            path: RoutePaths.staff,
            builder: (context, state) => const StaffListScreen(),
          ),
          GoRoute(
            path: RoutePaths.addStaff,
            builder: (context, state) => const AddStaffScreen(),
          ),
          GoRoute(
            path: RoutePaths.permissionMatrix,
            builder: (context, state) => const PermissionMatrixScreen(),
          ),

          // Settings
          GoRoute(
            path: RoutePaths.gymProfile,
            builder: (context, state) => const GymProfileScreen(),
          ),
          GoRoute(
            path: RoutePaths.branding,
            builder: (context, state) => const BrandingScreen(),
          ),
          GoRoute(
            path: RoutePaths.amenities,
            builder: (context, state) => const AmenitiesScreen(),
          ),
          GoRoute(
            path: RoutePaths.notificationSettings,
            builder: (context, state) => const NotificationSettingsScreen(),
          ),
          GoRoute(
            path: RoutePaths.seasonalHours,
            builder: (context, state) => const SeasonalHoursScreen(),
          ),
          GoRoute(
            path: RoutePaths.gymSections,
            builder: (context, state) => const GymSectionsScreen(),
          ),
          GoRoute(
            path: RoutePaths.sessionPolicies,
            builder: (context, state) => const SessionPoliciesScreen(),
          ),
          GoRoute(
            path: RoutePaths.kioskPin,
            builder: (context, state) => const KioskPinScreen(),
          ),
          GoRoute(
            path: RoutePaths.testingMode,
            builder: (context, state) => const TestingModeScreen(),
          ),
        ],
      ),

      // Kiosk — full screen, no shell
      GoRoute(
        path: RoutePaths.kiosk,
        builder: (context, state) => const CheckInKioskScreen(),
      ),
    ],
  );
});

/// Main shell with bottom navigation bar.
class MainShell extends ConsumerStatefulWidget {
  final Widget child;

  const MainShell({super.key, required this.child});

  @override
  ConsumerState<MainShell> createState() => _MainShellState();
}

class _MainShellState extends ConsumerState<MainShell> {
  int _currentIndex = 0;

  static const _mainRoutes = [
    RoutePaths.dashboard,
    RoutePaths.members,
    RoutePaths.trainers,
    RoutePaths.classes,
  ];

  @override
  Widget build(BuildContext context) {
    final isSuspended = ref.watch(gymSuspendedProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    // Update index based on current route. Match each tab to its route
    // prefix so pushed sub-screens (e.g. /members/:id) still highlight the
    // parent tab. Everything else falls through to the "More" tab.
    final currentLocation = GoRouterState.of(context).matchedLocation;
    int matchedIndex = _mainRoutes.length; // default: More
    for (int i = 0; i < _mainRoutes.length; i++) {
      final route = _mainRoutes[i];
      final isDashboard = route == RoutePaths.dashboard;
      if (isDashboard) {
        if (currentLocation == route) {
          matchedIndex = i;
          break;
        }
      } else if (currentLocation == route ||
          currentLocation.startsWith('$route/')) {
        matchedIndex = i;
        break;
      }
    }
    _currentIndex = matchedIndex;

    Widget body = widget.child;

    // Wrap with suspension banner for staff (read-only mode, not full block)
    if (isSuspended) {
      body = GymSuspensionOverlay(
        isStaffApp: true,
        child: body,
      );
    }

    return Scaffold(
      body: body,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) {
          if (index < _mainRoutes.length) {
            context.go(_mainRoutes[index]);
          } else {
            _showMoreMenu(context, isArabic);
          }
          setState(() => _currentIndex = index);
        },
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.dashboard_outlined),
            selectedIcon: const Icon(Icons.dashboard),
            label: isArabic ? 'الرئيسية' : 'Dashboard',
          ),
          NavigationDestination(
            icon: const Icon(Icons.people_outline),
            selectedIcon: const Icon(Icons.people),
            label: isArabic ? 'الاعضاء' : 'Members',
          ),
          NavigationDestination(
            icon: const Icon(Icons.fitness_center_outlined),
            selectedIcon: const Icon(Icons.fitness_center),
            label: isArabic ? 'المدربين' : 'Trainers',
          ),
          NavigationDestination(
            icon: const Icon(Icons.event_outlined),
            selectedIcon: const Icon(Icons.event),
            label: isArabic ? 'الحصص' : 'Classes',
          ),
          NavigationDestination(
            icon: const Icon(Icons.more_horiz),
            selectedIcon: const Icon(Icons.more_horiz),
            label: isArabic ? 'المزيد' : 'More',
          ),
        ],
      ),
    );
  }

  void _showMoreMenu(BuildContext context, bool isArabic) {
    final role = ref.read(staffRoleProvider) ?? '';

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _MoreMenuItem(
                icon: Icons.sports_gymnastics,
                label: isArabic ? 'التمارين' : 'Exercises',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.exercises);
                },
              ),
              _MoreMenuItem(
                icon: Icons.monetization_on_outlined,
                label: isArabic ? 'لوحة الايرادات' : 'Revenue Dashboard',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.revenueDashboard);
                },
              ),
              _MoreMenuItem(
                icon: Icons.attach_money,
                label: isArabic ? 'تسجيل دفع' : 'Record Payment',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.recordPayment);
                },
              ),
              _MoreMenuItem(
                icon: Icons.card_membership_outlined,
                label: isArabic ? 'الباقات النشطة' : 'Active Packages',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.activePackages);
                },
              ),
              if (StaffPermissions.hasPermission(
                  role, StaffPermission.assignPackage))
                _MoreMenuItem(
                  icon: Icons.assignment_outlined,
                  label: isArabic ? 'تعيين باقة' : 'Assign Package',
                  onTap: () {
                    Navigator.pop(context);
                    context.push(RoutePaths.assignPackage);
                  },
                ),
              _MoreMenuItem(
                icon: Icons.layers_outlined,
                label: isArabic ? 'قوالب الباقات' : 'Package Templates',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.packageTemplates);
                },
              ),
              _MoreMenuItem(
                icon: Icons.campaign_outlined,
                label: isArabic ? 'الاعلانات' : 'Announcements',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.announcements);
                },
              ),
              _MoreMenuItem(
                icon: Icons.qr_code_scanner,
                label: isArabic ? 'تسجيل الحضور' : 'Check-in',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.checkInLog);
                },
              ),
              _MoreMenuItem(
                icon: Icons.leaderboard_outlined,
                label: isArabic ? 'لوحة المدربين' : 'Coach Leaderboard',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.coachLeaderboard);
                },
              ),
              _MoreMenuItem(
                icon: Icons.emoji_events_outlined,
                label: isArabic ? 'ترتيب الاعضاء' : 'Member Leaderboard',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.memberLeaderboard);
                },
              ),
              _MoreMenuItem(
                icon: Icons.reviews_outlined,
                label: isArabic ? 'اعتدال الشهادات' : 'Moderate Testimonials',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.moderateTestimonials);
                },
              ),
              if (StaffPermissions.hasPermission(
                  role, StaffPermission.viewStaff))
                _MoreMenuItem(
                  icon: Icons.admin_panel_settings_outlined,
                  label: isArabic ? 'الموظفين' : 'Staff',
                  onTap: () {
                    Navigator.pop(context);
                    context.push(RoutePaths.staff);
                  },
                ),
              _MoreMenuItem(
                icon: Icons.settings_outlined,
                label: isArabic ? 'الاعدادات' : 'Settings',
                onTap: () {
                  Navigator.pop(context);
                  context.push(RoutePaths.gymProfile);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MoreMenuItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _MoreMenuItem({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon),
      title: Text(label),
      onTap: onTap,
    );
  }
}
