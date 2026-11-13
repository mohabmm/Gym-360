import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'providers.dart';
import 'router.dart';

/// Trainer app: flags come from `GET /feature-flags/me` through the shared service.
FeatureFlagService createTrainerFeatureFlagService(Ref ref) => ApiFeatureFlagService(
      fetch: () => ref.read(apiClientProvider).get<Map<String, dynamic>>(
            ApiEndpoints.featureFlagsMe,
            parser: (data) => Map<String, dynamic>.from(data as Map),
          ),
    );

/// Which flag owns each route. Deep links go through the same go_router redirect.
const trainerRouteGuard = FeatureRouteGuard(
  homePath: AppRoutes.home,
  routeFlags: {
    AppRoutes.workouts: FeatureFlagKeys.trainingWorkouts,
    AppRoutes.programBuilder: FeatureFlagKeys.trainingPrograms,
    AppRoutes.schedule: FeatureFlagKeys.schedulingBooking,
    '/sessions': FeatureFlagKeys.schedulingBooking,
    AppRoutes.chatList: FeatureFlagKeys.communicationChat,
    AppRoutes.broadcast: FeatureFlagKeys.communicationAnnouncements,
    AppRoutes.formCheck: FeatureFlagKeys.progressPhotos,
    '/more/nutrition': FeatureFlagKeys.nutrition,
    AppRoutes.clientPackages: FeatureFlagKeys.ptPackages,
    AppRoutes.earningsDashboard: FeatureFlagKeys.gymFinance,
    AppRoutes.myRatings: FeatureFlagKeys.ptCoachRatings,
  },
);

/// Client sub-pages are /clients/:id/<page>; map them by suffix.
const _clientSubPageFlags = {
  'onboarding': FeatureFlagKeys.gymMemberRecords,
  'notes': FeatureFlagKeys.gymMemberRecords,
  'assign-workout': FeatureFlagKeys.trainingWorkouts,
  'measurements': FeatureFlagKeys.ptProgressMeasurements,
};

/// Redirect target for [location] when it belongs to a disabled feature.
String? trainerFeatureRedirect(String location, FeatureFlags flags) {
  final segments = Uri.parse(location).pathSegments;
  if (segments.length == 3 && segments.first == 'clients') {
    final flag = _clientSubPageFlags[segments[2]];
    if (flag != null && !flags.isEnabled(flag)) return AppRoutes.home;
  }
  return trainerRouteGuard.redirect(location, flags);
}

/// Loads cached flags at start, refreshes when a gym becomes active, and forgets them on logout.
class TrainerFeatureFlagsSync extends ConsumerStatefulWidget {
  final Widget child;
  const TrainerFeatureFlagsSync({super.key, required this.child});

  @override
  ConsumerState<TrainerFeatureFlagsSync> createState() => _TrainerFeatureFlagsSyncState();
}

class _TrainerFeatureFlagsSyncState extends ConsumerState<TrainerFeatureFlagsSync> {
  @override
  void initState() {
    super.initState();
    final controller = ref.read(featureFlagsProvider.notifier);
    controller.start().then((_) {
      final gym = ref.read(activeGymIdProvider);
      if (gym != null && gym.isNotEmpty) controller.refresh();
    });
  }

  @override
  Widget build(BuildContext context) {
    ref.listen<String?>(activeGymIdProvider, (previous, next) {
      if (next != null && next.isNotEmpty && next != previous) {
        ref.read(featureFlagsProvider.notifier).refresh();
      }
    });
    ref.listen<AuthState>(authProvider, (previous, next) {
      if (previous?.status == AuthStatus.authenticated && next.status == AuthStatus.unauthenticated) {
        ref.read(featureFlagsProvider.notifier).reset();
      }
    });
    return widget.child;
  }
}
