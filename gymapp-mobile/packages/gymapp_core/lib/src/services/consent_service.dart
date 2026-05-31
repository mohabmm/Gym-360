import 'package:hive/hive.dart';
import '../constants/storage_keys.dart';

/// Manages PDPL (Egypt Personal Data Protection Law) consent.
/// Tracks data processing consent, marketing consent, and provides
/// data export and account deletion capabilities.
class ConsentService {
  late Box _consentBox;

  Future<void> initialize() async {
    _consentBox = await Hive.openBox(StorageKeys.consentBox);
  }

  /// Check if user has given data processing consent.
  bool hasDataProcessingConsent() {
    return _consentBox.get(StorageKeys.dataProcessingConsent, defaultValue: false)
        as bool;
  }

  /// Check if user has given marketing consent.
  bool hasMarketingConsent() {
    return _consentBox.get(StorageKeys.marketingConsent, defaultValue: false)
        as bool;
  }

  /// Get the timestamp when consent was given.
  DateTime? getConsentTimestamp() {
    final timestamp =
        _consentBox.get(StorageKeys.consentTimestamp) as String?;
    if (timestamp == null) return null;
    return DateTime.tryParse(timestamp);
  }

  /// Record data processing consent.
  Future<void> recordDataProcessingConsent(bool granted) async {
    await _consentBox.put(StorageKeys.dataProcessingConsent, granted);
    await _consentBox.put(
      StorageKeys.consentTimestamp,
      DateTime.now().toUtc().toIso8601String(),
    );
  }

  /// Record marketing consent.
  Future<void> recordMarketingConsent(bool granted) async {
    await _consentBox.put(StorageKeys.marketingConsent, granted);
  }

  /// Get all stored consent data for local reference.
  Map<String, dynamic> getConsentData() {
    return {
      'dataProcessingConsent': hasDataProcessingConsent(),
      'marketingConsent': hasMarketingConsent(),
      'consentTimestamp': getConsentTimestamp()?.toIso8601String(),
    };
  }

  /// Clear all consent data (used on account deletion).
  Future<void> clearConsent() async {
    await _consentBox.clear();
  }

  /// Build consent records for API submission.
  List<Map<String, dynamic>> buildConsentRecords({
    required bool dataProcessing,
    required bool marketing,
  }) {
    final now = DateTime.now().toUtc().toIso8601String();
    return [
      {
        'consentType': 'DataProcessing',
        'isGranted': dataProcessing,
        'consentedAt': now,
        'consentText':
            'I consent to the processing of my personal data in accordance with the Egyptian Personal Data Protection Law (PDPL).',
      },
      {
        'consentType': 'Marketing',
        'isGranted': marketing,
        'consentedAt': now,
        'consentText':
            'I consent to receiving marketing communications from GymApp.',
      },
    ];
  }
}
