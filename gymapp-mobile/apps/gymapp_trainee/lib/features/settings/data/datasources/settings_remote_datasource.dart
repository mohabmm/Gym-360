import 'package:gymapp_core/gymapp_core.dart';

class SettingsRemoteDataSource {
  final ApiClient _apiClient;

  SettingsRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE PUT /identity/profile returns 200 with no body.
  /// We call putVoid then re-fetch the profile to return the updated user.
  Future<UserModel> updateProfile({
    required String userId,
    required UserProfileUpdateRequest request,
  }) async {
    final body = <String, dynamic>{
      if (request.firstName != null) 'firstName': request.firstName,
      if (request.lastName != null) 'lastName': request.lastName,
      if (request.phone != null) 'phone': request.phone,
      if (request.photoUrl != null) 'profilePhotoUrl': request.photoUrl,
      if (request.dateOfBirth != null)
        'dateOfBirth': request.dateOfBirth!.toIso8601String().split('T')[0],
      if (request.gender != null) 'gender': request.gender,
    };
    await _apiClient.putVoid(
      ApiEndpoints.profile,
      data: body,
    );
    // Re-fetch updated profile since BE returns no body
    return _apiClient.get<UserModel>(
      ApiEndpoints.profile,
      parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
    );
  }

  /// NOTE: No dedicated GET /identity/consent endpoint on BE.
  /// BE only has POST /identity/consent (RecordConsentEndpoint).
  /// UserModel also lacks a `consents` field.
  /// Returns empty list until BE adds a GET consent listing endpoint.
  Future<List<UserConsentModel>> getConsents() async {
    // TODO: Implement when BE adds GET /identity/consent endpoint.
    return const [];
  }

  /// BE POST /identity/consent expects consentType, isGranted, consentText.
  Future<void> updateConsent(
    String consentType,
    bool isGranted, {
    String consentText = '',
  }) async {
    await _apiClient.postVoid(
      ApiEndpoints.consent,
      data: {
        'consentType': consentType,
        'isGranted': isGranted,
        'consentText': consentText,
      },
    );
  }

  Future<void> downloadMyData() async {
    await _apiClient.get<dynamic>(
      ApiEndpoints.myData,
      parser: (data) => data,
    );
  }

  Future<void> deleteAccount() async {
    await _apiClient.delete(ApiEndpoints.deleteAccount);
  }

  Future<void> changePassword(
      String currentPassword, String newPassword) async {
    await _apiClient.postVoid(
      ApiEndpoints.changePassword,
      data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
  }
}
