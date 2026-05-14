import 'package:gymapp_core/gymapp_core.dart';

import 'fake_secure_storage.dart';
import 'mock_http_adapter.dart';

/// Bundle of test scaffolding returned by [buildTestApiClient].
class TestApiClient {
  TestApiClient({
    required this.apiClient,
    required this.adapter,
    required this.storage,
  });

  final ApiClient apiClient;
  final MockHttpAdapter adapter;
  final FakeSecureStorage storage;

  /// Shortcut for the single recorded request when a test expects exactly one.
  /// Fails fast with a helpful error if the count is wrong.
  RecordedRequest get onlyRequest {
    if (adapter.recorded.length != 1) {
      throw StateError(
        'Expected exactly 1 HTTP request, got ${adapter.recorded.length}: '
        '${adapter.recorded}',
      );
    }
    return adapter.recorded.single;
  }
}

/// Builds an [ApiClient] with the real interceptor stack (auth, gym-id, error)
/// wired up, pointed at a [MockHttpAdapter].
///
/// Seeds the in-memory secure storage with a token + gym id so every request
/// carries the Authorization + X-Gym-Id headers the BE expects.
TestApiClient buildTestApiClient({
  String accessToken = 'test-jwt',
  String refreshToken = 'test-refresh',
  String userId = 'user-1',
  String gymId = 'gym-1',
  String baseUrl = 'https://test.api.gymapp.local',
}) {
  final storage = FakeSecureStorage(
    accessToken: accessToken,
    refreshToken: refreshToken,
    userId: userId,
    currentGymId: gymId,
  );
  final client = ApiClient(
    baseUrl: baseUrl,
    storage: storage,
    enableLogging: false,
  );
  final adapter = MockHttpAdapter();
  client.dio.httpClientAdapter = adapter;
  return TestApiClient(apiClient: client, adapter: adapter, storage: storage);
}
