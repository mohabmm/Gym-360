import 'package:gymapp_core/gymapp_core.dart';

/// In-memory [SecureStorageService] for tests — avoids the flutter_secure_storage
/// platform channel, which isn't available in `flutter test`.
///
/// [SecureStorageService] routes every named accessor through [read],
/// [write], [deleteKey] and [clearAll], so overriding those four is enough.
class FakeSecureStorage extends SecureStorageService {
  /// Starts with the given entries already stored.
  FakeSecureStorage([Map<SecureStorageKey, String> seed = const {}]) {
    _values.addAll(seed);
  }

  final Map<SecureStorageKey, String> _values = {};

  @override
  Future<String?> read(SecureStorageKey key) async => _values[key];

  @override
  Future<void> write(SecureStorageKey key, String value) async =>
      _values[key] = value;

  @override
  Future<void> deleteKey(SecureStorageKey key) async => _values.remove(key);

  @override
  Future<void> clearAll() async => _values.clear();
}
