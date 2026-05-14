import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers EarningsDashboardScreen and ClientPackagesScreen.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('EarningsDashboardScreen', () {
    test('GET /billing/trainers/{id}/earnings with gymId + month range',
        () async {
      t.adapter.onGet(
        ApiEndpoints.trainerEarnings(kTrainerId),
        respond: (_) => t.adapter.ok(Samples.trainerEarnings()),
      );

      final earnings = await t.apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.trainerEarnings(kTrainerId),
        queryParameters: {
          'gymId': kGymId,
          'from': '2026-04-01',
          'to': '2026-04-16',
        },
        parser: (d) => d as Map<String, dynamic>,
      );

      final req = t.onlyRequest;
      expect(req.path, '/api/v1/billing/trainers/$kTrainerId/earnings');
      expect(req.q('gymId'), kGymId);
      expect(RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(req.q('from')!), isTrue);
      expect(RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(req.q('to')!), isTrue);
      expect(earnings['totalSessions'], 24);
      expect((earnings['totalEarningsEgp'] as num).toDouble(), 4800.0);
      expect(earnings['currency'], 'EGP');
      expect((earnings['records'] as List).single['memberId'], 'client-1');
    });
  });

  group('ClientPackagesScreen', () {
    test('GET /billing/packages/my-clients for trainer', () async {
      t.adapter.onGet(
        ApiEndpoints.myClientPackages,
        respond: (_) => t.adapter.ok([Samples.sessionPackage()]),
      );

      final packages = await t.apiClient.get<List<SessionPackageModel>>(
        ApiEndpoints.myClientPackages,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) => (data as List<dynamic>)
            .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.path, '/api/v1/billing/packages/my-clients');
      expect(req.q('trainerId'), kTrainerId);
      expect(req.q('gymId'), kGymId);
      expect(packages.single.status, 'Active');
      expect(packages.single.remainingSessions, 7);
    });

    test('empty { items: [] } envelope parses to empty list', () async {
      t.adapter.onGet(
        ApiEndpoints.myClientPackages,
        respond: (_) => t.adapter.ok({'items': <Map<String, dynamic>>[]}),
      );

      final packages = await t.apiClient.get<List<SessionPackageModel>>(
        ApiEndpoints.myClientPackages,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) {
          final list = data is List
              ? data
              : (data is Map && data['items'] is List)
                  ? data['items'] as List<dynamic>
                  : const [];
          return list
              .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
      );

      expect(packages, isEmpty);
    });
  });
}
