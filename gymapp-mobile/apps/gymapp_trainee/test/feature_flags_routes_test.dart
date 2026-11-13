import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/feature_flags.dart';
import 'package:gymapp_trainee/core/router.dart';

void main() {
  final pt = FeatureFlags.ptDefaults();
  final business = FeatureFlags(
    values: {for (final k in traineeRouteGuard.routeFlags.values) k: true},
    preset: 'business',
    origin: FeatureFlagsOrigin.server,
  );

  group('pt_module: OFF features cannot be reached by navigation or deep link', () {
    for (final path in [
      AppRoutes.workouts,
      '/workouts/history',
      AppRoutes.bookSession,
      AppRoutes.classSchedule,
      AppRoutes.nutrition,
      AppRoutes.waterTracker,
      AppRoutes.chat,
      AppRoutes.announcements,
      AppRoutes.leaderboard,
      AppRoutes.checkin,
      AppRoutes.progressPhotos,
      '/more/rate-trainer/abc',
    ]) {
      test('$path → home', () => expect(traineeRouteGuard.redirect(path, pt), AppRoutes.home));
    }
  });

  group('pt_module: PT screens stay reachable', () {
    for (final path in [
      AppRoutes.home,
      AppRoutes.progress,
      AppRoutes.logWeight,
      AppRoutes.logMeasurements,
      AppRoutes.schedule,
      '/schedule/sessions/abc',
      AppRoutes.membership,
      AppRoutes.notifications,
      AppRoutes.settings,
    ]) {
      test(path, () => expect(traineeRouteGuard.redirect(path, pt), isNull));
    }
  });

  test('business preset opens everything', () {
    for (final path in [AppRoutes.nutrition, AppRoutes.classSchedule, AppRoutes.chat, AppRoutes.workouts]) {
      expect(traineeRouteGuard.redirect(path, business), isNull, reason: path);
    }
  });
}
