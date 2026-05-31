import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('PlanFeaturesModel.isEnabled', () {
    test('returns true for explicitly enabled features', () {
      const model = PlanFeaturesModel(
        gymId: 'gym-1',
        planCode: 'pro',
        planName: 'Pro',
        memberLimit: 500,
        trainerLimit: 20,
        staffLimit: 10,
        features: [
          PlanFeatureItemModel(
            featureCode: 'nutrition_tracking',
            featureName: 'Nutrition tracking',
            isEnabled: true,
          ),
          PlanFeatureItemModel(
            featureCode: 'remote_workout',
            featureName: 'Remote workout',
            isEnabled: true,
          ),
          PlanFeatureItemModel(
            featureCode: 'meal_plans',
            featureName: 'Meal plans',
            isEnabled: false,
          ),
        ],
      );
      expect(model.isEnabled('nutrition_tracking'), isTrue);
      expect(model.isEnabled('remote_workout'), isTrue);
    });

    test('returns false for explicitly disabled features', () {
      const model = PlanFeaturesModel(
        gymId: 'gym-1',
        planCode: 'free',
        planName: 'Free',
        memberLimit: 50,
        trainerLimit: 1,
        staffLimit: 1,
        features: [
          PlanFeatureItemModel(
            featureCode: 'meal_plans',
            featureName: 'Meal plans',
            isEnabled: false,
          ),
        ],
      );
      expect(model.isEnabled('meal_plans'), isFalse);
    });

    test('returns false when feature is missing (fails closed)', () {
      const model = PlanFeaturesModel(
        gymId: 'gym-1',
        planCode: 'free',
        planName: 'Free',
        memberLimit: 50,
        trainerLimit: 1,
        staffLimit: 1,
        features: [],
      );
      expect(model.isEnabled('remote_workout'), isFalse);
    });
  });

  group('PlanFeaturesModel JSON', () {
    test('round-trips through fromJson/toJson', () {
      final json = {
        'gymId': 'gym-1',
        'planCode': 'pro',
        'planName': 'Pro',
        'memberLimit': 500,
        'trainerLimit': 20,
        'staffLimit': 10,
        'features': [
          {
            'featureCode': 'remote_workout',
            'featureName': 'Remote workout',
            'isEnabled': true,
            'limitValue': null,
            'isOverridden': false,
          },
        ],
      };
      final model = PlanFeaturesModel.fromJson(json);
      expect(model.planCode, 'pro');
      expect(model.memberLimit, 500);
      expect(model.isEnabled('remote_workout'), isTrue);
      expect(model.isEnabled('missing_feature'), isFalse);
    });
  });
}
