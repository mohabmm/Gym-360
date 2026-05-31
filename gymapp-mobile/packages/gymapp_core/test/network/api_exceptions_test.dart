import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('ApiException.toFailure', () {
    test('no status code -> NetworkFailure', () {
      const ex = ApiException(message: 'dropped');
      expect(ex.toFailure(), isA<NetworkFailure>());
    });

    test('status code maps through ServerFailure', () {
      const ex = ApiException(message: 'x', statusCode: 500);
      final f = ex.toFailure();
      expect(f, isA<ServerFailure>());
      expect((f as ServerFailure).statusCode, 500);
      expect(f.code, 'SERVER_ERROR');
    });
  });

  group('UnauthorizedException', () {
    test('status 401', () {
      const ex = UnauthorizedException();
      expect(ex.statusCode, 401);
      expect(ex.errorCode, 'UNAUTHORIZED');
    });
  });

  group('FeatureGatedException', () {
    test('toFailure returns FeatureGatedFailure', () {
      const ex = FeatureGatedException(
        featureCode: 'nutrition_tracking',
        requiredPlan: 'Pro',
      );
      final f = ex.toFailure();
      expect(f, isA<FeatureGatedFailure>());
      expect((f as FeatureGatedFailure).featureCode, 'nutrition_tracking');
      expect(f.requiredPlan, 'Pro');
    });
  });

  group('StorageLimitException', () {
    test('toFailure returns StorageLimitFailure with counts', () {
      const ex = StorageLimitException(
        currentCount: 20,
        maxCount: 20,
        resourceType: 'photos',
      );
      final f = ex.toFailure();
      expect(f, isA<StorageLimitFailure>());
      expect((f as StorageLimitFailure).currentCount, 20);
      expect(f.maxCount, 20);
      expect(f.resourceType, 'photos');
    });
  });

  group('GymSuspendedException', () {
    test('toFailure returns GymSuspendedFailure', () {
      const ex = GymSuspendedException();
      expect(ex.toFailure(), isA<GymSuspendedFailure>());
    });
  });
}
