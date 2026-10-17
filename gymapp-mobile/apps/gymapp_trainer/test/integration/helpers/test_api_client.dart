import 'package:gymapp_core/gymapp_core.dart';

import 'fake_secure_storage.dart';
import 'mock_http_adapter.dart';

const String kTrainerId = 'trainer-1';
const String kGymId = 'gym-1';
const String kClientId = 'client-1';
const String kAccessToken = 'test-jwt';

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
  RecordedRequest get onlyRequest {
    if (adapter.recorded.length != 1) {
      throw StateError(
        'Expected exactly 1 HTTP request, got ${adapter.recorded.length}: '
        '${adapter.recorded}',
      );
    }
    return adapter.recorded.single;
  }

  /// Returns the first recorded request matching a method+path. Fails if none.
  RecordedRequest requestWhere({String? method, String? path}) {
    return adapter.recorded.firstWhere(
      (r) =>
          (method == null || r.method == method.toUpperCase()) &&
          (path == null || r.path == path),
      orElse: () => throw StateError(
        'No recorded request matched method=$method path=$path.\n'
        'Recorded: ${adapter.recorded}',
      ),
    );
  }
}

/// Credentials + gym context seeded into [FakeSecureStorage].
class TestSession {
  const TestSession({
    this.accessToken = kAccessToken,
    this.refreshToken = 'test-refresh',
    this.userId = kTrainerId,
    this.gymId = kGymId,
  });

  /// No tokens, user or gym — for flows that run before authentication.
  static const anonymous =
      TestSession(accessToken: '', refreshToken: '', userId: '', gymId: '');

  final String accessToken;
  final String refreshToken;
  final String userId;
  final String gymId;
}

/// Builds an [ApiClient] with the real interceptor stack (auth, gym-id, error)
/// wired up, pointed at a [MockHttpAdapter].
///
/// Seeds in-memory secure storage with trainer-context values so every request
/// carries `Authorization: Bearer ...` + `X-Gym-Id: ...` headers the BE expects.
TestApiClient buildTestApiClient({
  TestSession session = const TestSession(),
  String baseUrl = 'https://test.api.gymapp.local',
}) {
  final storage = FakeSecureStorage(
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
    userId: session.userId,
    currentGymId: session.gymId,
    userRole: 'Trainer',
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
