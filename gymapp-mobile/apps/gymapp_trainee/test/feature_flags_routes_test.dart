import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/feature_flags.dart';
import 'package:gymapp_trainee/core/router.dart';
import 'package:gymapp_trainee/features/plan/presentation/plan_routes.dart';

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
      AppRoutes.inBodyScan,
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
      AppRoutes.exerciseProgress,
      PlanRoutes.plan,
      PlanRoutes.workout('d-1'),
      AppRoutes.notifications,
      AppRoutes.settings,
    ]) {
      test(path, () => expect(traineeRouteGuard.redirect(path, pt), isNull));
    }
  });

  test('with pt.training_plan off, the plan and a workout deep link go home', () {
    final off = FeatureFlags(
      values: {...pt.values, FeatureFlagKeys.ptTrainingPlan: false},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );
    expect(traineeRouteGuard.redirect(PlanRoutes.plan, off), AppRoutes.home);
    expect(traineeRouteGuard.redirect(PlanRoutes.workout('d-1'), off), AppRoutes.home);
  });

  test('business preset opens everything', () {
    for (final path in [AppRoutes.nutrition, AppRoutes.classSchedule, AppRoutes.chat, AppRoutes.workouts, AppRoutes.inBodyScan]) {
      expect(traineeRouteGuard.redirect(path, business), isNull, reason: path);
    }
  });
}
