import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../constants/storage_keys.dart';

/// Wraps flutter_secure_storage for token and sensitive data storage.
class SecureStorageService {
  final FlutterSecureStorage _storage;

  SecureStorageService({FlutterSecureStorage? storage})
      : _storage = storage ??
            const FlutterSecureStorage(
              aOptions: AndroidOptions(encryptedSharedPreferences: true),
              iOptions: IOSOptions(
                accessibility: KeychainAccessibility.unlocked,
              ),
            );

  // Access Token
  Future<String?> getAccessToken() =>
      _storage.read(key: StorageKeys.accessToken);

  Future<void> saveAccessToken(String token) =>
      _storage.write(key: StorageKeys.accessToken, value: token);

  // Refresh Token
  Future<String?> getRefreshToken() =>
      _storage.read(key: StorageKeys.refreshToken);

  Future<void> saveRefreshToken(String token) =>
      _storage.write(key: StorageKeys.refreshToken, value: token);

  // Save both tokens at once
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await Future.wait([
      saveAccessToken(accessToken),
      saveRefreshToken(refreshToken),
    ]);
  }

  // Clear tokens on logout
  Future<void> clearTokens() async {
    await Future.wait([
      _storage.delete(key: StorageKeys.accessToken),
      _storage.delete(key: StorageKeys.refreshToken),
    ]);
  }

  // User ID
  Future<String?> getUserId() => _storage.read(key: StorageKeys.userId);

  Future<void> saveUserId(String id) =>
      _storage.write(key: StorageKeys.userId, value: id);

  // Current Gym ID (for X-Gym-Id header)
  Future<String?> getCurrentGymId() =>
      _storage.read(key: StorageKeys.currentGymId);

  Future<void> saveCurrentGymId(String gymId) =>
      _storage.write(key: StorageKeys.currentGymId, value: gymId);

  // User Role
  Future<String?> getUserRole() => _storage.read(key: StorageKeys.userRole);

  Future<void> saveUserRole(String role) =>
      _storage.write(key: StorageKeys.userRole, value: role);

  // FCM Token
  Future<String?> getFcmToken() => _storage.read(key: StorageKeys.fcmToken);

  Future<void> saveFcmToken(String token) =>
      _storage.write(key: StorageKeys.fcmToken, value: token);

  // Clear all on logout
  Future<void> clearAll() => _storage.deleteAll();

  // Generic read/write
  Future<String?> read(String key) => _storage.read(key: key);

  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);

  Future<void> deleteKey(String key) => _storage.delete(key: key);

  Future<bool> hasKey(String key) async {
    final value = await _storage.read(key: key);
    return value != null;
  }
}
