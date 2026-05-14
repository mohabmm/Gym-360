import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/checkin/data/datasources/checkin_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Check-in screen: trainee displays their QR for staff to scan. The trainee
/// app itself only reads history — all writes happen via the staff kiosk.
void main() {
  late TestApiClient t;
  late CheckInRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = CheckInRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET /gyms/{gymId}/check-ins returns paginated history', () async {
    t.adapter.onGet(
      ApiEndpoints.checkInHistory('gym-1'),
      respond: (_) => t.adapter.ok([Samples.checkIn(), Samples.checkIn(id: 'ci-2')]),
    );

    final history = await ds.getCheckInHistory('gym-1', page: 2, pageSize: 5);
    expect(history, hasLength(2));
    expect(history.first.method, 'qr');
    expect(t.onlyRequest.q('page'), '2');
    expect(t.onlyRequest.q('pageSize'), '5');
  });
}
