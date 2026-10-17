import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../types/auth_tokens.dart';
import '../types/entity_ids.dart';
import 'secure_storage_key.dart';

/// Wraps flutter_secure_storage for token and sensitive data storage.
///
/// Every named accessor goes through the four [SecureStorageKey]-typed
/// primitives ([read], [write], [deleteKey], [clearAll]); a test double only
/// has to replace those.
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
  Future<String?> getAccessToken() => read(SecureStorageKey.accessToken);

  Future<void> saveAccessToken(AccessToken token) =>
      write(SecureStorageKey.accessToken, token);

  // Refresh Token
  Future<String?> getRefreshToken() => read(SecureStorageKey.refreshToken);

  Future<void> saveRefreshToken(RefreshToken token) =>
      write(SecureStorageKey.refreshToken, token);

  // Save both tokens at once
  Future<void> saveTokens({
    required AccessToken accessToken,
    required RefreshToken refreshToken,
  }) async {
    await Future.wait([
      saveAccessToken(accessToken),
      saveRefreshToken(refreshToken),
    ]);
  }

  // Clear tokens on logout
  Future<void> clearTokens() async {
    await Future.wait([
      deleteKey(SecureStorageKey.accessToken),
      deleteKey(SecureStorageKey.refreshToken),
    ]);
  }

  // User ID
  Future<String?> getUserId() => read(SecureStorageKey.userId);

  Future<void> saveUserId(UserId id) => write(SecureStorageKey.userId, id);

  // Current Gym ID (for X-Gym-Id header)
  Future<String?> getCurrentGymId() => read(SecureStorageKey.currentGymId);

  Future<void> saveCurrentGymId(GymId gymId) =>
      write(SecureStorageKey.currentGymId, gymId);

  // User Role
  Future<String?> getUserRole() => read(SecureStorageKey.userRole);

  Future<void> saveUserRole(String role) =>
      write(SecureStorageKey.userRole, role);

  // FCM Token
  Future<String?> getFcmToken() => read(SecureStorageKey.fcmToken);

  Future<void> saveFcmToken(String token) =>
      write(SecureStorageKey.fcmToken, token);

  // Clear all on logout
  Future<void> clearAll() => _storage.deleteAll();

  // Generic read/write
  Future<String?> read(SecureStorageKey key) => _storage.read(key: key.name);

  Future<void> write(SecureStorageKey key, String value) =>
      _storage.write(key: key.name, value: value);

  Future<void> deleteKey(SecureStorageKey key) =>
      _storage.delete(key: key.name);

  Future<bool> hasKey(SecureStorageKey key) async => await read(key) != null;
}
