import 'package:gymapp_core/gymapp_core.dart';

/// In-memory [SecureStorageService] for tests — avoids the flutter_secure_storage
/// platform channel, which isn't available in `flutter test`.
///
/// Overrides every accessor the trainee app's interceptors touch
/// ([AuthInterceptor], [GymIdInterceptor]). The parent still constructs a real
/// [FlutterSecureStorage] in its default constructor, but we never call into it.
class FakeSecureStorage extends SecureStorageService {
  FakeSecureStorage({
    String? accessToken,
    String? refreshToken,
    String? userId,
    String? currentGymId,
    String? userRole,
  }) {
    if (accessToken != null) _values['accessToken'] = accessToken;
    if (refreshToken != null) _values['refreshToken'] = refreshToken;
    if (userId != null) _values['userId'] = userId;
    if (currentGymId != null) _values['currentGymId'] = currentGymId;
    if (userRole != null) _values['userRole'] = userRole;
  }

  final Map<String, String> _values = {};

  @override
  Future<String?> getAccessToken() async => _values['accessToken'];

  @override
  Future<void> saveAccessToken(String token) async =>
      _values['accessToken'] = token;

  @override
  Future<String?> getRefreshToken() async => _values['refreshToken'];

  @override
  Future<void> saveRefreshToken(String token) async =>
      _values['refreshToken'] = token;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    _values['accessToken'] = accessToken;
    _values['refreshToken'] = refreshToken;
  }

  @override
  Future<void> clearTokens() async {
    _values.remove('accessToken');
    _values.remove('refreshToken');
  }

  @override
  Future<String?> getUserId() async => _values['userId'];

  @override
  Future<void> saveUserId(String id) async => _values['userId'] = id;

  @override
  Future<String?> getCurrentGymId() async => _values['currentGymId'];

  @override
  Future<void> saveCurrentGymId(String gymId) async =>
      _values['currentGymId'] = gymId;

  @override
  Future<String?> getUserRole() async => _values['userRole'];

  @override
  Future<void> saveUserRole(String role) async => _values['userRole'] = role;

  @override
  Future<String?> getFcmToken() async => _values['fcmToken'];

  @override
  Future<void> saveFcmToken(String token) async =>
      _values['fcmToken'] = token;

  @override
  Future<void> clearAll() async => _values.clear();

  @override
  Future<String?> read(String key) async => _values[key];

  @override
  Future<void> write(String key, String value) async => _values[key] = value;

  @override
  Future<void> deleteKey(String key) async => _values.remove(key);

  @override
  Future<bool> hasKey(String key) async => _values.containsKey(key);
}
