import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'providers.dart';
import 'router.dart';

/// Trainee app: flags come from `GET /feature-flags/me` through the shared service.
FeatureFlagService createTraineeFeatureFlagService(Ref ref) => ApiFeatureFlagService(
      fetch: () => ref.read(apiClientProvider).get<Map<String, dynamic>>(
            ApiEndpoints.featureFlagsMe,
            parser: (data) => Map<String, dynamic>.from(data as Map),
          ),
    );

/// Which flag owns each route. Deep links go through the same go_router redirect.
const traineeRouteGuard = FeatureRouteGuard(
  homePath: AppRoutes.home,
  routeFlags: {
    AppRoutes.workouts: FeatureFlagKeys.trainingWorkouts,
    AppRoutes.guidedWorkout: FeatureFlagKeys.trainingRemoteWorkout,
    AppRoutes.progress: FeatureFlagKeys.ptProgressMeasurements,
    AppRoutes.progressPhotos: FeatureFlagKeys.progressPhotos,
    AppRoutes.schedule: FeatureFlagKeys.ptDualSessionLogging,
    AppRoutes.bookSession: FeatureFlagKeys.schedulingBooking,
    AppRoutes.classSchedule: FeatureFlagKeys.classes,
    AppRoutes.nutrition: FeatureFlagKeys.nutrition,
    AppRoutes.chat: FeatureFlagKeys.communicationChat,
    AppRoutes.announcements: FeatureFlagKeys.communicationAnnouncements,
    AppRoutes.leaderboard: FeatureFlagKeys.gamificationMemberLeaderboard,
    '/more/rate-trainer': FeatureFlagKeys.ptCoachRatings,
    AppRoutes.checkin: FeatureFlagKeys.gymAttendance,
    AppRoutes.membership: FeatureFlagKeys.ptPackages,
    AppRoutes.exerciseProgress: FeatureFlagKeys.ptExerciseProgress,
  },
);

/// Loads cached flags at start, refreshes after login / gym link, and forgets them on logout.
class TraineeFeatureFlagsSync extends ConsumerStatefulWidget {
  final Widget child;
  const TraineeFeatureFlagsSync({super.key, required this.child});

  @override
  ConsumerState<TraineeFeatureFlagsSync> createState() => _TraineeFeatureFlagsSyncState();
}

class _TraineeFeatureFlagsSyncState extends ConsumerState<TraineeFeatureFlagsSync> {
  @override
  void initState() {
    super.initState();
    final controller = ref.read(featureFlagsProvider.notifier);
    controller.start().then((_) {
      if (_hasGym(ref.read(authProvider).valueOrNull)) controller.refresh();
    });
  }

  static bool _hasGym(AuthState? auth) =>
      auth?.status == AuthStatus.authenticated && (auth?.currentGymId?.isNotEmpty ?? false);

  @override
  Widget build(BuildContext context) {
    ref.listen<AsyncValue<AuthState>>(authProvider, (previous, next) {
      final controller = ref.read(featureFlagsProvider.notifier);
      final before = previous?.valueOrNull;
      final now = next.valueOrNull;
      if (now?.status == AuthStatus.unauthenticated && before?.status == AuthStatus.authenticated) {
        controller.reset();
      } else if (_hasGym(now) && (!_hasGym(before) || before?.currentGymId != now?.currentGymId)) {
        controller.refresh();
      }
    });
    return widget.child;
  }
}
