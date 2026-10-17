import '../constants/storage_keys.dart';
import '../types/entity_ids.dart';

/// A key in the platform secure store (Keychain / EncryptedSharedPreferences).
///
/// The set of keys is closed: the constructor is private, so only the
/// well-known keys below (and the per-gym kiosk PIN hash) can be read or
/// written — no free-form strings reach [SecureStorageService]. The wire
/// names come from [StorageKeys], so values already persisted on devices keep
/// resolving.
extension type const SecureStorageKey._(String name) {
  static const accessToken = SecureStorageKey._(StorageKeys.accessToken);
  static const refreshToken = SecureStorageKey._(StorageKeys.refreshToken);
  static const userId = SecureStorageKey._(StorageKeys.userId);
  static const currentGymId = SecureStorageKey._(StorageKeys.currentGymId);
  static const userRole = SecureStorageKey._(StorageKeys.userRole);
  static const fcmToken = SecureStorageKey._(StorageKeys.fcmToken);

  /// Per-gym cached kiosk PIN hash (`kiosk_pin_hash:{gymId}`).
  SecureStorageKey.kioskPinHash(GymId gymId)
      : this._(StorageKeys.kioskPinHashFor(gymId));
}
