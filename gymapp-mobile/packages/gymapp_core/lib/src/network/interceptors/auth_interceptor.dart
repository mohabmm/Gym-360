import 'package:dio/dio.dart';
import '../../constants/api_endpoints.dart';
import '../../services/secure_storage_service.dart';

/// Injects JWT access token into every request.
/// Handles 401 responses by attempting token refresh.
class AuthInterceptor extends Interceptor {
  final SecureStorageService _storage;
  final Dio _dio;
  bool _isRefreshing = false;

  /// Endpoints that never carry a bearer token. A 401 from one of these is a
  /// credential/OTP failure for the caller to display, not an expired session.
  static const _publicPaths = [
    ApiEndpoints.login,
    ApiEndpoints.register,
    ApiEndpoints.sendOtp,
    ApiEndpoints.verifyOtp,
    ApiEndpoints.forgotPassword,
    ApiEndpoints.resetPassword,
    ApiEndpoints.refreshToken,
    ApiEndpoints.socialLogin,
  ];

  /// Optional callback fired when the refresh flow fails (refresh token
  /// expired/revoked). Apps wire this to their auth notifier so the user is
  /// signed out and redirected to login instead of being stuck on screens
  /// with stale "Error loading" indicators.
  final Future<void> Function()? _onAuthInvalidated;

  AuthInterceptor({
    required SecureStorageService storage,
    required Dio dio,
    Future<void> Function()? onAuthInvalidated,
  })  : _storage = storage,
        _dio = dio,
        _onAuthInvalidated = onAuthInvalidated;

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Skip auth for public endpoints
    if (_publicPaths.contains(options.path)) {
      return handler.next(options);
    }

    final token = await _storage.getAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }

    return handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode != 401 || _isRefreshing) {
      return handler.next(err);
    }

    // Wrong password / bad OTP comes back as 401 too. Running the refresh flow
    // here would fire onAuthInvalidated and wipe the login form before the
    // screen can show the error.
    if (_publicPaths.contains(err.requestOptions.path)) {
      return handler.next(err);
    }

    _isRefreshing = true;

    try {
      final refreshToken = await _storage.getRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) {
        _isRefreshing = false;
        await _storage.clearTokens();
        await _onAuthInvalidated?.call();
        return handler.next(err);
      }

      // BE schema: RefreshTokenEndpoint.Request expects `{ "token": "..." }`.
      // Using any other key hits the 400 "Token required" validation and
      // silently logs the user out.
      final response = await _dio.post(
        ApiEndpoints.refreshToken,
        data: {'token': refreshToken},
      );

      if (response.statusCode == 200) {
        final newAccessToken = response.data['data']['accessToken'] as String;
        final newRefreshToken =
            response.data['data']['refreshToken'] as String;

        await _storage.saveTokens(
          accessToken: newAccessToken,
          refreshToken: newRefreshToken,
        );

        // Retry the original request with new token
        final opts = err.requestOptions;
        opts.headers['Authorization'] = 'Bearer $newAccessToken';

        final retryResponse = await _dio.fetch(opts);
        _isRefreshing = false;
        return handler.resolve(retryResponse);
      }

      // Refresh returned non-200 — treat as expired/invalid session.
      await _storage.clearTokens();
      await _onAuthInvalidated?.call();
    } catch (e) {
      // Network error or refresh rejected (400/401/403). Clear tokens and
      // signal the auth notifier so it can navigate the user back to login.
      await _storage.clearTokens();
      await _onAuthInvalidated?.call();
    }

    _isRefreshing = false;
    return handler.next(err);
  }
}
