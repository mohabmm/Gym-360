import 'dart:developer' as developer;

import 'package:dio/dio.dart';
import '../constants/app_constants.dart';
import '../models/api_response_model.dart';
import '../models/pagination_model.dart';
import '../services/secure_storage_service.dart';
import 'api_exceptions.dart';
import 'interceptors/auth_interceptor.dart';
import 'interceptors/gym_id_interceptor.dart';
import 'interceptors/locale_interceptor.dart';
import 'interceptors/logging_interceptor.dart';
import 'interceptors/error_interceptor.dart';
import '../services/telemetry_service.dart';

/// Centralized HTTP client wrapping Dio.
/// Configured with JWT auth, X-Gym-Id header, logging, and error mapping.
class ApiClient {
  late final Dio _dio;
  final SecureStorageService _storage;
  final TelemetryService? _telemetry;

  ApiClient({
    required String baseUrl,
    required SecureStorageService storage,
    bool enableLogging = true,
    Future<void> Function()? onAuthInvalidated,
    TelemetryService? telemetry,
  })  : _storage = storage,
        _telemetry = telemetry {
    _dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(
          seconds: AppConstants.apiConnectTimeoutSeconds,
        ),
        receiveTimeout: const Duration(
          seconds: AppConstants.apiTimeoutSeconds,
        ),
        sendTimeout: const Duration(
          seconds: AppConstants.apiTimeoutSeconds,
        ),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    // Order matters: logging first, then auth, then gym id, then locale,
    // then error handling. Locale is per-request (reads Hive each time) so
    // switching language in Settings takes effect without recreating the
    // client.
    // Added when console logging is on OR a telemetry sink is wired, so error
    // forwarding to SigNoz works even in release builds (where enableLogging
    // is typically off to avoid console spam).
    if (enableLogging || _telemetry != null) {
      _dio.interceptors.add(LoggingInterceptor(
        telemetry: _telemetry,
        consoleLogging: enableLogging,
      ));
    }
    _dio.interceptors.add(
      AuthInterceptor(
        storage: _storage,
        dio: _dio,
        onAuthInvalidated: onAuthInvalidated,
      ),
    );
    _dio.interceptors.add(
      GymIdInterceptor(storage: _storage),
    );
    _dio.interceptors.add(LocaleInterceptor());
    _dio.interceptors.add(ErrorInterceptor());
  }

  Dio get dio => _dio;

  /// Unwraps any error thrown from a Dio call into an ApiException so
  /// callers never see raw DioException or Dart runtime errors.
  ///
  /// ErrorInterceptor rejects with DioException(error: ApiException(...)).
  /// Dio wraps any other throw inside the request pipeline the same way.
  /// This method collapses every shape back to a single ApiException.
  Never _throwAsApi(Object e, [StackTrace? stackTrace]) {
    if (e is ApiException) throw e;

    if (e is DioException) {
      final inner = e.error;
      if (inner is ApiException) throw inner;
      if (inner is TypeError || inner is FormatException) {
        _logParseError(
          inner!,
          e.stackTrace,
          path: e.requestOptions.path,
          body: e.response?.data,
        );
        throw const ApiException(
          message: 'The server returned data in an unexpected format. '
              'Please update the app or contact support.',
          errorCode: 'PARSE_ERROR',
        );
      }
      // DioException with no mapped inner error — should be rare after
      // ErrorInterceptor runs, but keep a safe fallback.
      throw ApiException(
        message: e.message ?? 'Network request failed.',
        errorCode: 'NETWORK_ERROR',
      );
    }

    if (e is TypeError ||
        e is FormatException ||
        e.toString().contains('is not a subtype of type')) {
      _logParseError(e, stackTrace);
      throw const ApiException(
        message: 'The server returned data in an unexpected format. '
            'Please update the app or contact support.',
        errorCode: 'PARSE_ERROR',
      );
    }

    throw ApiException(message: e.toString());
  }

  void _logParseError(
    Object error,
    StackTrace? stackTrace, {
    String? path,
    Object? body,
  }) {
    developer.log(
      'Response parse failed${path != null ? ' for $path' : ''}: $error',
      name: 'ApiClient',
      error: error,
      stackTrace: stackTrace,
      level: 1000, // SEVERE
    );
    if (body != null) {
      developer.log(
        'Offending payload: $body',
        name: 'ApiClient',
        level: 900, // WARNING
      );
    }
    // Ship to SigNoz. Body is deliberately NOT forwarded (may carry PII).
    _telemetry?.recordError(
      error,
      stackTrace,
      context: 'Response parse failed',
      attributes: {
        if (path != null) 'http.path': path,
        'error.code': 'PARSE_ERROR',
      },
    );
  }

  /// GET request returning parsed data.
  Future<T> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic data) parser,
    CancelToken? cancelToken,
  }) async {
    try {
      final response = await _dio.get(
        path,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
      return parser(response.data['data'] ?? response.data);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// GET request returning paginated data.
  Future<PaginatedResult<T>> getPaginated<T>(
    String path, {
    required PaginationParams params,
    Map<String, dynamic>? additionalParams,
    required T Function(Map<String, dynamic> json) itemParser,
    CancelToken? cancelToken,
  }) async {
    try {
      final queryParams = {
        ...params.toQueryParams(),
        ...?additionalParams,
      };

      final response = await _dio.get(
        path,
        queryParameters: queryParams,
        cancelToken: cancelToken,
      );

      final rawResponse = ApiResponseRaw.fromJson(
        response.data as Map<String, dynamic>,
      );

      // BE returns ApiResponse<PagedResponse<T>>: data is either a bare list
      // (legacy) or an object with { items, page, pageSize, totalCount,
      // totalPages }. Support both.
      final rawData = rawResponse.data;
      final List<dynamic> rawItems;
      PaginationMeta? metaFromBody;
      if (rawData is List) {
        rawItems = rawData;
      } else if (rawData is Map<String, dynamic> && rawData['items'] is List) {
        rawItems = rawData['items'] as List<dynamic>;
        metaFromBody = PaginationMeta(
          totalCount: (rawData['totalCount'] as num?)?.toInt() ??
              rawItems.length,
          pageSize:
              (rawData['pageSize'] as num?)?.toInt() ?? params.pageSize,
          currentPage: (rawData['page'] as num?)?.toInt() ?? params.page,
          totalPages: (rawData['totalPages'] as num?)?.toInt() ?? 1,
        );
      } else {
        rawItems = const [];
      }

      // Parse each row independently: a single malformed item (e.g. a backend
      // ghost row with null required fields) must not blow up the whole list.
      // Skip the bad row, log it, and keep the rest.
      final items = <T>[];
      for (final item in rawItems) {
        try {
          items.add(itemParser(item as Map<String, dynamic>));
        } catch (e) {
          developer.log(
            'Skipping unparseable list item from $path: $e',
            name: 'ApiClient',
          );
        }
      }

      final meta = rawResponse.meta != null
          ? PaginationMeta.fromJson(rawResponse.meta!)
          : metaFromBody ??
              PaginationMeta(
                totalCount: items.length,
                pageSize: params.pageSize,
                currentPage: params.page,
                totalPages: 1,
              );

      return PaginatedResult(items: items, meta: meta);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// POST request.
  Future<T> post<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic data) parser,
    CancelToken? cancelToken,
  }) async {
    try {
      final response = await _dio.post(
        path,
        data: data,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
      return parser(response.data['data'] ?? response.data);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// POST request returning void.
  Future<void> postVoid(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    CancelToken? cancelToken,
  }) async {
    try {
      await _dio.post(
        path,
        data: data,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// PUT request.
  Future<T> put<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic data) parser,
    CancelToken? cancelToken,
  }) async {
    try {
      final response = await _dio.put(
        path,
        data: data,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
      return parser(response.data['data'] ?? response.data);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// PUT request returning void.
  Future<void> putVoid(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    CancelToken? cancelToken,
  }) async {
    try {
      await _dio.put(
        path,
        data: data,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// PATCH request.
  Future<T> patch<T>(
    String path, {
    dynamic data,
    required T Function(dynamic data) parser,
    CancelToken? cancelToken,
  }) async {
    try {
      final response = await _dio.patch(
        path,
        data: data,
        cancelToken: cancelToken,
      );
      return parser(response.data['data'] ?? response.data);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// DELETE request.
  Future<void> delete(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    CancelToken? cancelToken,
  }) async {
    try {
      await _dio.delete(
        path,
        data: data,
        queryParameters: queryParameters,
        cancelToken: cancelToken,
      );
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// Upload file with multipart.
  Future<T> upload<T>(
    String path, {
    required FormData formData,
    required T Function(dynamic data) parser,
    void Function(int, int)? onSendProgress,
    CancelToken? cancelToken,
  }) async {
    try {
      final response = await _dio.post(
        path,
        data: formData,
        onSendProgress: onSendProgress,
        cancelToken: cancelToken,
        options: Options(
          headers: {'Content-Type': 'multipart/form-data'},
        ),
      );
      return parser(response.data['data'] ?? response.data);
    } catch (e, st) {
      _throwAsApi(e, st);
    }
  }

  /// Download file.
  Future<Response> download(
    String path,
    String savePath, {
    void Function(int, int)? onReceiveProgress,
    CancelToken? cancelToken,
  }) async {
    return _dio.download(
      path,
      savePath,
      onReceiveProgress: onReceiveProgress,
      cancelToken: cancelToken,
    );
  }
}
