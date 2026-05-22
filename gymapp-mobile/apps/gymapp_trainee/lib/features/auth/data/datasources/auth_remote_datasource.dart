import 'package:gymapp_core/gymapp_core.dart';

class AuthRemoteDataSource {
  final ApiClient _apiClient;

  AuthRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  Future<AuthResponse> login(LoginRequest request) async {
    return _apiClient.post<AuthResponse>(
      ApiEndpoints.login,
      data: request.toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<AuthResponse> register(RegisterRequest request) async {
    return _apiClient.post<AuthResponse>(
      ApiEndpoints.register,
      data: request.toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<void> sendOtp(OtpRequest request) async {
    await _apiClient.postVoid(
      ApiEndpoints.sendOtp,
      data: request.toJson(),
    );
  }

  Future<void> verifyOtp(VerifyOtpRequest request) async {
    await _apiClient.postVoid(
      ApiEndpoints.verifyOtp,
      data: request.toJson(),
    );
  }

  Future<UserModel> getProfile() async {
    return _apiClient.get<UserModel>(
      ApiEndpoints.profile,
      parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
    );
  }

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
    return getProfile();
  }

  Future<void> linkGym(LinkGymRequest request) async {
    await _apiClient.postVoid(
      ApiEndpoints.linkGym,
      data: request.toJson(),
    );
  }

  /// NOTE: No BE endpoint for social login yet. This will throw a 404.
  /// Kept as a stub for future implementation.
  Future<AuthResponse> socialLogin(SocialLoginRequest request) async {
    return _apiClient.post<AuthResponse>(
      ApiEndpoints.socialLogin,
      data: request.toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<void> forgotPassword(ForgotPasswordRequest request) async {
    await _apiClient.postVoid(
      ApiEndpoints.forgotPassword,
      data: request.toJson(),
    );
  }

  /// BE expects `consentType`, `isGranted`, and `consentText`.
  Future<void> recordConsent({
    required String consentType,
    required bool isGranted,
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
}
