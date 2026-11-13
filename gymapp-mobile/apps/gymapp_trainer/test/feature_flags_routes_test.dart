import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/feature_flags.dart';
import 'package:gymapp_trainer/core/router.dart';

void main() {
  final pt = FeatureFlags.ptDefaults();

  group('pt_module: OFF features cannot be reached by navigation or deep link', () {
    for (final path in [
      AppRoutes.workouts,
      AppRoutes.workoutBuilder,
      AppRoutes.programBuilder,
      AppRoutes.schedule,
      AppRoutes.availability,
      AppRoutes.bookingRequests,
      '/sessions/abc/live',
      AppRoutes.chatList,
      '/more/chat/xyz',
      AppRoutes.broadcast,
      AppRoutes.formCheck,
      AppRoutes.mealPlanBuilder,
      AppRoutes.earningsDashboard,
      AppRoutes.myRatings,
      '/clients/c1/notes',
      '/clients/c1/onboarding',
      '/clients/c1/assign-workout',
    ]) {
      test('$path → home', () => expect(trainerFeatureRedirect(path, pt), AppRoutes.home));
    }
  });

  group('pt_module: PT screens stay reachable', () {
    for (final path in [
      AppRoutes.home,
      AppRoutes.clients,
      '/clients/c1',
      '/clients/c1/measurements',
      AppRoutes.clientPackages,
      AppRoutes.notifications,
      AppRoutes.settings,
      AppRoutes.profile,
    ]) {
      test(path, () => expect(trainerFeatureRedirect(path, pt), isNull));
    }
  });

  test('measurements are blocked when pt.progress_measurements is off', () {
    final noMeasurements = FeatureFlags(
      values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: k != FeatureFlagKeys.ptProgressMeasurements},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );
    expect(trainerFeatureRedirect('/clients/c1/measurements', noMeasurements), AppRoutes.home);
  });
}
