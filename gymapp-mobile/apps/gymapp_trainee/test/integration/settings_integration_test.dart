import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/settings/data/datasources/settings_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Settings screen — profile update, PDPL consent, My Data export,
/// Delete Account, Change Password stub.
void main() {
  late TestApiClient t;
  late SettingsRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = SettingsRemoteDataSource(apiClient: t.apiClient);
  });

  test('PUT /identity/profile then GET refetch', () async {
    t.adapter.onPut(
      ApiEndpoints.profile,
      respond: (_) => t.adapter.noContent(),
    );
    t.adapter.onGet(
      ApiEndpoints.profile,
      respond: (_) => t.adapter.ok(Samples.user()),
    );
    final updated = await ds.updateProfile(
      userId: 'user-1',
      request: const UserProfileUpdateRequest(firstName: 'Mona'),
    );
    expect(updated.firstName, 'Mona');
    expect(t.adapter.recorded.map((r) => r.method).toList(), ['PUT', 'GET']);
  });

  test('getConsents() returns empty list (no BE endpoint yet)', () async {
    expect(await ds.getConsents(), isEmpty);
    expect(t.adapter.recorded, isEmpty);
  });

  test('POST /identity/consent for PDPL toggles', () async {
    t.adapter.onPost(
      ApiEndpoints.consent,
      respond: (_) => t.adapter.noContent(),
    );
    await ds.updateConsent('Marketing', false, consentText: 'v2');
    expect(t.onlyRequest.bodyAsMap, {
      'consentType': 'Marketing',
      'isGranted': false,
      'consentText': 'v2',
    });
  });

  test('GET /identity/my-data — PDPL export', () async {
    t.adapter.onGet(
      ApiEndpoints.myData,
      respond: (_) => t.adapter.ok({'profile': Samples.user(), 'workouts': []}),
    );
    await ds.downloadMyData();
    expect(t.onlyRequest.method, 'GET');
  });

  test('DELETE /identity/account — PDPL deletion', () async {
    t.adapter.onDelete(
      ApiEndpoints.deleteAccount,
      respond: (_) => t.adapter.noContent(),
    );
    await ds.deleteAccount();
    expect(t.onlyRequest.method, 'DELETE');
  });

  test('changePassword() stub — BE returns 404', () async {
    t.adapter.onPost(
      ApiEndpoints.changePassword,
      respond: (_) => t.adapter.error(404, code: 'NOT_FOUND'),
    );
    await expectLater(
      ds.changePassword('old', 'new'),
      throwsA(isA<NotFoundException>()),
    );
  });
}
