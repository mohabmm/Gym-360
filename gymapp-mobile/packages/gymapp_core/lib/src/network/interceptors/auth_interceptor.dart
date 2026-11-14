import 'package:dio/dio.dart';
import '../../constants/api_endpoints.dart';
import '../../services/secure_storage_service.dart';
import '../../types/auth_tokens.dart';

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

  /// Sent by the API when the token is valid but carries no gym the request can be scoped to
  /// (e.g. an owner session issued before a gym was selected, or a staff token issued before
  /// the gym membership was linked). A fresh token fixes it, so it is handled like a 401.
  static const gymNotFoundCode = 'GYM_NOT_FOUND';

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    // Wrong password / bad OTP comes back as 401 too. Running the refresh flow
    // here would fire onAuthInvalidated and wipe the login form before the
    // screen can show the error.
    if (_isRefreshing || !_needsFreshToken(err) || _publicPaths.contains(err.requestOptions.path)) {
      return handler.next(err);
    }

    _isRefreshing = true;
    final newAccessToken = await _refreshTokens();
    if (newAccessToken == null) {
      // Refresh token missing, expired or revoked: the session is over.
      await _storage.clearTokens();
      await _onAuthInvalidated?.call();
      _isRefreshing = false;
      return handler.next(err);
    }

    // Retry the original request once with the new token. If the retry fails, the caller sees
    // that error; the session itself is still valid, so the user is not signed out.
    final opts = err.requestOptions;
    opts.headers['Authorization'] = 'Bearer $newAccessToken';
    try {
      return handler.resolve(await _dio.fetch(opts));
    } on DioException catch (retryError) {
      return handler.next(retryError);
    } finally {
      _isRefreshing = false;
    }
  }

  static bool _needsFreshToken(DioException err) {
    final status = err.response?.statusCode;
    return status == 401 || (status == 404 && _errorCode(err.response?.data) == gymNotFoundCode);
  }

  static String? _errorCode(Object? body) {
    if (body is! Map) return null;
    final errors = body['errors'];
    if (errors is! List || errors.isEmpty) return null;
    final first = errors.first;
    return first is Map ? first['code']?.toString() : null;
  }

  /// Exchanges the stored refresh token for a new pair. Returns the new access token, or null
  /// when there is no refresh token or the API rejects it.
  Future<String?> _refreshTokens() async {
    final refreshToken = await _storage.getRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) return null;

    try {
      // BE schema: RefreshTokenEndpoint.Request expects `{ "token": "..." }`.
      // Using any other key hits the 400 "Token required" validation and
      // silently logs the user out.
      final response = await _dio.post(ApiEndpoints.refreshToken, data: {'token': refreshToken});
      if (response.statusCode != 200) return null;

      final data = response.data['data'];
      final newAccessToken = data['accessToken'] as String;
      await _storage.saveTokens(
        accessToken: AccessToken(newAccessToken),
        refreshToken: RefreshToken(data['refreshToken'] as String),
      );
      return newAccessToken;
    } catch (_) {
      // Network error, rejected refresh (400/401/403) or an unexpected body.
      return null;
    }
  }
}
