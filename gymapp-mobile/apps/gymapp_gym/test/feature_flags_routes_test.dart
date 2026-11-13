import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/feature_flags.dart';
import 'package:gymapp_gym/core/router.dart';

void main() {
  final pt = FeatureFlags.ptDefaults();

  group('pt_module: OFF features cannot be reached by navigation or deep link', () {
    for (final path in [
      RoutePaths.classes,
      '/classes/c1',
      RoutePaths.exercises,
      RoutePaths.kiosk,
      RoutePaths.manualCheckIn,
      RoutePaths.checkInLog,
      RoutePaths.recordPayment,
      RoutePaths.revenueDashboard,
      RoutePaths.announcements,
      RoutePaths.coachLeaderboard,
      '/gamification/ratings/t1',
      RoutePaths.moderateTestimonials,
      RoutePaths.memberLeaderboard,
      RoutePaths.permissionMatrix,
      RoutePaths.branding,
      RoutePaths.kioskPin,
    ]) {
      test('$path → dashboard', () => expect(staffRouteGuard.redirect(path, pt), RoutePaths.dashboard));
    }
  });

  group('pt_module: PT screens stay reachable', () {
    for (final path in [
      RoutePaths.dashboard,
      RoutePaths.members,
      '/members/m1',
      RoutePaths.trainers,
      RoutePaths.assignPackage,
      RoutePaths.activePackages,
      RoutePaths.packageTemplates,
      RoutePaths.ptTrials,
      RoutePaths.ptReview,
      RoutePaths.ptDashboard,
      RoutePaths.ptInterests,
      RoutePaths.staff,
      RoutePaths.gymProfile,
    ]) {
      test(path, () => expect(staffRouteGuard.redirect(path, pt), isNull));
    }
  });

  test('interest list is closed when pt.package_interest is off', () {
    final off = FeatureFlags(
      values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: k != FeatureFlagKeys.ptPackageInterest},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );
    expect(staffRouteGuard.redirect(RoutePaths.ptInterests, off), RoutePaths.dashboard);
  });
}
