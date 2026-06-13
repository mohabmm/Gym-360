import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('ServerFailure.fromStatusCode', () {
    test('maps 400 -> BAD_REQUEST', () {
      final f = ServerFailure.fromStatusCode(400);
      expect(f.code, 'BAD_REQUEST');
      expect(f.statusCode, 400);
    });

    test('maps 401 -> UNAUTHORIZED', () {
      expect(ServerFailure.fromStatusCode(401).code, 'UNAUTHORIZED');
    });

    test('maps 403 -> FORBIDDEN', () {
      expect(ServerFailure.fromStatusCode(403).code, 'FORBIDDEN');
    });

    test('maps 404 -> NOT_FOUND', () {
      expect(ServerFailure.fromStatusCode(404).code, 'NOT_FOUND');
    });

    test('maps 409 -> CONFLICT', () {
      expect(ServerFailure.fromStatusCode(409).code, 'CONFLICT');
    });

    test('maps 422 -> VALIDATION', () {
      expect(ServerFailure.fromStatusCode(422).code, 'VALIDATION');
    });

    test('maps 429 -> RATE_LIMITED', () {
      expect(ServerFailure.fromStatusCode(429).code, 'RATE_LIMITED');
    });

    test('maps 500 -> SERVER_ERROR', () {
      expect(ServerFailure.fromStatusCode(500).code, 'SERVER_ERROR');
    });

    test('unknown status -> UNKNOWN', () {
      expect(ServerFailure.fromStatusCode(418).code, 'UNKNOWN');
    });

    test('includes Arabic message', () {
      expect(ServerFailure.fromStatusCode(401).messageAr, isNotNull);
    });

    test('custom message overrides default', () {
      final f = ServerFailure.fromStatusCode(400, message: 'custom');
      expect(f.message, 'custom');
    });
  });

  group('FeatureGatedFailure', () {
    test('message includes required plan', () {
      const failure = FeatureGatedFailure(
        featureCode: 'remote_workout',
        requiredPlan: 'Pro',
      );
      expect(failure.message, contains('Pro'));
      expect(failure.code, 'FEATURE_GATED');
    });

    test('props include featureCode and requiredPlan for equatable', () {
      const a = FeatureGatedFailure(
          featureCode: 'remote_workout', requiredPlan: 'Pro');
      const b = FeatureGatedFailure(
          featureCode: 'remote_workout', requiredPlan: 'Pro');
      const c = FeatureGatedFailure(
          featureCode: 'nutrition_tracking', requiredPlan: 'Pro');
      expect(a, equals(b));
      expect(a, isNot(equals(c)));
    });
  });

  group('StorageLimitFailure', () {
    test('message includes counts', () {
      const failure = StorageLimitFailure(
        currentCount: 20,
        maxCount: 20,
        resourceType: 'progress photos',
      );
      expect(failure.message, contains('20/20'));
      expect(failure.code, 'STORAGE_LIMIT');
    });
  });

  group('Failure equality', () {
    test('NetworkFailure is const-equal', () {
      expect(const NetworkFailure(), equals(const NetworkFailure()));
    });

    test('GymSuspendedFailure has bilingual messages', () {
      const failure = GymSuspendedFailure();
      expect(failure.message, isNotEmpty);
      expect(failure.messageAr, isNotEmpty);
      expect(failure.code, 'GYM_SUSPENDED');
    });
  });

  group('ValidationFailure', () {
    test('holds fieldErrors', () {
      const failure = ValidationFailure(fieldErrors: {'email': 'required'});
      expect(failure.fieldErrors['email'], 'required');
      expect(failure.code, 'VALIDATION');
    });
  });
}
