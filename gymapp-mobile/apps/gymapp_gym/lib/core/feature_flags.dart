import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'providers.dart';
import 'router.dart';

/// Staff mobile app: flags come from `GET /feature-flags/me` through the shared service.
FeatureFlagService createStaffFeatureFlagService(Ref ref) =>
    ApiFeatureFlagService(
      fetch: () => ref.read(apiClientProvider).get<Map<String, dynamic>>(
            ApiEndpoints.featureFlagsMe,
            parser: (data) => Map<String, dynamic>.from(data as Map),
          ),
    );

/// Which flag owns each route. Deep links go through the same go_router redirect.
const staffRouteGuard = FeatureRouteGuard(
  homePath: RoutePaths.dashboard,
  routeFlags: {
    RoutePaths.classes: FeatureFlagKeys.classes,
    RoutePaths.exercises: FeatureFlagKeys.trainingWorkouts,
    RoutePaths.kiosk: FeatureFlagKeys.gymAttendance,
    '/checkin': FeatureFlagKeys.gymAttendance,
    RoutePaths.recordPayment: FeatureFlagKeys.gymFinance,
    RoutePaths.revenueDashboard: FeatureFlagKeys.gymFinance,
    RoutePaths.packageTemplates: FeatureFlagKeys.ptPackages,
    RoutePaths.assignPackage: FeatureFlagKeys.ptPackages,
    RoutePaths.activePackages: FeatureFlagKeys.ptPackages,
    RoutePaths.announcements: FeatureFlagKeys.communicationAnnouncements,
    RoutePaths.coachLeaderboard: FeatureFlagKeys.coachLeaderboard,
    '/gamification/ratings': FeatureFlagKeys.ptCoachRatings,
    RoutePaths.moderateTestimonials: FeatureFlagKeys.coachTestimonials,
    RoutePaths.memberLeaderboard: FeatureFlagKeys.gamificationMemberLeaderboard,
    RoutePaths.permissionMatrix: FeatureFlagKeys.staffCustomPermissions,
    RoutePaths.branding: FeatureFlagKeys.gymBranding,
    RoutePaths.kioskPin: FeatureFlagKeys.gymAttendance,
    RoutePaths.ptTrials: FeatureFlagKeys.ptTrialEndAlerts,
    RoutePaths.ptReview: FeatureFlagKeys.ptDualSessionLogging,
    RoutePaths.ptDashboard: FeatureFlagKeys.ptOwnerDashboard,
  },
);

/// Loads cached flags at start, refreshes after login / gym switch, and forgets them on logout.
class StaffFeatureFlagsSync extends ConsumerStatefulWidget {
  final Widget child;
  const StaffFeatureFlagsSync({super.key, required this.child});

  @override
  ConsumerState<StaffFeatureFlagsSync> createState() =>
      _StaffFeatureFlagsSyncState();
}

class _StaffFeatureFlagsSyncState extends ConsumerState<StaffFeatureFlagsSync> {
  @override
  void initState() {
    super.initState();
    final controller = ref.read(featureFlagsProvider.notifier);
    controller.start().then((_) {
      if (_hasGym(ref.read(authProvider).valueOrNull)) controller.refresh();
    });
  }

  static bool _hasGym(AuthState? auth) =>
      auth?.status == AuthStatus.authenticated &&
      (auth?.currentGymId?.isNotEmpty ?? false);

  @override
  Widget build(BuildContext context) {
    ref.listen<AsyncValue<AuthState>>(authProvider, (previous, next) {
      final controller = ref.read(featureFlagsProvider.notifier);
      final before = previous?.valueOrNull;
      final now = next.valueOrNull;
      if (now?.status == AuthStatus.unauthenticated &&
          before?.status == AuthStatus.authenticated) {
        controller.reset();
      } else if (_hasGym(now) &&
          (!_hasGym(before) || before?.currentGymId != now?.currentGymId)) {
        controller.refresh();
      }
    });
    return widget.child;
  }
}
