import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('FeatureCodes.requiredPlanFor', () {
    test('core features are Free', () {
      expect(FeatureCodes.requiredPlanFor(FeatureCodes.memberManagement),
          FeatureCodes.planFree);
      expect(FeatureCodes.requiredPlanFor(FeatureCodes.qrCheckIn),
          FeatureCodes.planFree);
      expect(FeatureCodes.requiredPlanFor(FeatureCodes.basicAnalytics),
          FeatureCodes.planFree);
      expect(FeatureCodes.requiredPlanFor(FeatureCodes.basicAnnouncements),
          FeatureCodes.planFree);
    });

    test('Starter-tier features', () {
      for (final code in [
        FeatureCodes.workoutBuilder,
        FeatureCodes.workoutTemplates,
        FeatureCodes.sessionBooking,
        FeatureCodes.classManagement,
        FeatureCodes.progressTracking,
        FeatureCodes.sessionFeedback,
        FeatureCodes.trainerManagement,
        FeatureCodes.paymentRecording,
        FeatureCodes.receipts,
        FeatureCodes.sessionPackages,
      ]) {
        expect(FeatureCodes.requiredPlanFor(code), FeatureCodes.planStarter,
            reason: '$code should require Starter');
      }
    });

    test('Pro-tier features', () {
      for (final code in [
        FeatureCodes.nutritionTracking,
        FeatureCodes.mealPlans,
        FeatureCodes.remoteWorkout,
        FeatureCodes.multiWeekPrograms,
        FeatureCodes.leaderboards,
        FeatureCodes.trainerRatings,
        FeatureCodes.gymBranding,
        FeatureCodes.advancedAnalytics,
        FeatureCodes.churnAlerts,
        FeatureCodes.coachLeaderboard,
        FeatureCodes.testimonialModeration,
        FeatureCodes.mealPlanBuilder,
      ]) {
        expect(FeatureCodes.requiredPlanFor(code), FeatureCodes.planPro,
            reason: '$code should require Pro');
      }
    });

    test('Business-tier features', () {
      for (final code in [
        FeatureCodes.whiteLabel,
        FeatureCodes.apiAccess,
        FeatureCodes.customExerciseLibrary,
        FeatureCodes.pdfReports,
        FeatureCodes.churnAi,
        FeatureCodes.customPermissions,
        FeatureCodes.multiLocation,
      ]) {
        expect(FeatureCodes.requiredPlanFor(code), FeatureCodes.planBusiness,
            reason: '$code should require Business');
      }
    });

    test('unknown code defaults to Business (highest tier)', () {
      expect(FeatureCodes.requiredPlanFor('some_future_feature'),
          FeatureCodes.planBusiness);
    });
  });
}
