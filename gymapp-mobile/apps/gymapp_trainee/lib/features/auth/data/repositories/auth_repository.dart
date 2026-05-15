import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/auth_remote_datasource.dart';

class AuthRepository {
  final AuthRemoteDataSource _remoteDataSource;

  AuthRepository({required AuthRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<AuthResponse> login(LoginRequest request) =>
      _remoteDataSource.login(request);

  Future<AuthResponse> register(RegisterRequest request) =>
      _remoteDataSource.register(request);

  Future<void> sendOtp(OtpRequest request) =>
      _remoteDataSource.sendOtp(request);

  Future<void> verifyOtp(VerifyOtpRequest request) =>
      _remoteDataSource.verifyOtp(request);

  Future<UserModel> getProfile() => _remoteDataSource.getProfile();

  Future<UserModel> updateProfile({
    required String userId,
    required UserProfileUpdateRequest request,
  }) =>
      _remoteDataSource.updateProfile(userId: userId, request: request);

  Future<void> linkGym(LinkGymRequest request) =>
      _remoteDataSource.linkGym(request);

  Future<AuthResponse> socialLogin(SocialLoginRequest request) =>
      _remoteDataSource.socialLogin(request);

  Future<void> forgotPassword(ForgotPasswordRequest request) =>
      _remoteDataSource.forgotPassword(request);

  Future<void> recordConsent({
    required String consentType,
    required bool isGranted,
    String consentText = '',
  }) =>
      _remoteDataSource.recordConsent(
        consentType: consentType,
        isGranted: isGranted,
        consentText: consentText,
      );
}
