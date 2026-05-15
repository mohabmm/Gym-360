import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/settings_remote_datasource.dart';

class SettingsRepository {
  final SettingsRemoteDataSource _remoteDataSource;

  SettingsRepository({required SettingsRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<UserModel> updateProfile({
    required String userId,
    required UserProfileUpdateRequest request,
  }) =>
      _remoteDataSource.updateProfile(userId: userId, request: request);

  Future<List<UserConsentModel>> getConsents() =>
      _remoteDataSource.getConsents();

  Future<void> updateConsent(
    String consentType,
    bool isGranted, {
    String consentText = '',
  }) =>
      _remoteDataSource.updateConsent(consentType, isGranted,
          consentText: consentText);

  Future<void> downloadMyData() => _remoteDataSource.downloadMyData();

  Future<void> deleteAccount() => _remoteDataSource.deleteAccount();

  Future<void> changePassword(
          String currentPassword, String newPassword) =>
      _remoteDataSource.changePassword(currentPassword, newPassword);
}
