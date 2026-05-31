import 'package:dio/dio.dart';
import '../api_exceptions.dart';

/// Maps DioException and HTTP status codes to typed ApiExceptions with
/// human-readable messages extracted from the backend's standard envelope:
///   { "data": ..., "errors": [ { "code": "X", "message": "Y", "meta": {...} } ], "meta": ... }
///
/// Error codes are aligned with docs/01_GYMAPP_BACKEND_COMPLETE.md and
/// docs/PRD_TRAINEE_ME_ENDPOINTS.md.
class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    // Reject with a DioException whose `.error` carries our typed ApiException.
    // ApiClient unwraps this so callers catch ApiException directly.
    final apiException = _mapToApiException(err);
    if (apiException == null) {
      return handler.next(err);
    }
    handler.reject(
      DioException(
        requestOptions: err.requestOptions,
        response: err.response,
        type: err.type,
        error: apiException,
        stackTrace: err.stackTrace,
      ),
    );
  }

  ApiException? _mapToApiException(DioException err) {
    final response = err.response;

    switch (err.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const ApiException(
          message: 'The server is taking too long to respond. Please try again.',
          statusCode: 408,
          errorCode: 'TIMEOUT',
        );

      case DioExceptionType.connectionError:
        return ApiException(
          message:
              'Could not reach the server. Check your internet connection '
              'or that the API host (${err.requestOptions.baseUrl}) is correct.',
          errorCode: 'NETWORK_ERROR',
        );

      case DioExceptionType.badResponse:
        if (response == null) {
          return ApiException(
            message: err.message ?? 'Empty response from server.',
            errorCode: 'NO_RESPONSE',
          );
        }

        final statusCode = response.statusCode ?? 500;
        final parsed = _parseEnvelope(response.data);
        final code = parsed.errorCode ?? '';

        if (code == 'PLAN.FEATURE_NOT_AVAILABLE') {
          final meta = parsed.firstMeta;
          return FeatureGatedException(
            featureCode: (meta?['feature'] as String?) ?? '',
            requiredPlan: (meta?['requiredPlan'] as String?) ?? '',
          );
        }

        if (code == 'PLAN.STORAGE_LIMIT_REACHED' || code == 'PLAN.LIMIT_EXCEEDED') {
          final meta = parsed.firstMeta;
          return StorageLimitException(
            currentCount: (meta?['currentUsage'] as int?) ?? 0,
            maxCount: (meta?['limit'] as num?)?.toInt() ?? 0,
            resourceType: (meta?['resource'] as String?) ?? 'items',
          );
        }

        if (code == 'GYM_SUSPENDED') {
          return const GymSuspendedException();
        }

        switch (statusCode) {
          case 400:
            return ValidationException(
              message: parsed.message,
              fieldErrors: parsed.fieldErrors,
              errors: parsed.messages,
            );
          case 401:
            return UnauthorizedException(message: parsed.message);
          case 403:
            return ForbiddenException(message: parsed.message);
          case 404:
            return NotFoundException(message: parsed.message);
          case 409:
            return ApiException(
              message: parsed.message,
              statusCode: 409,
              errorCode: parsed.errorCode ?? 'CONFLICT',
              errors: parsed.messages,
            );
          case 500:
          case 502:
          case 503:
          case 504:
            return ApiException(
              message: parsed.message.isEmpty
                  ? 'Server error. Please try again shortly.'
                  : parsed.message,
              statusCode: statusCode,
              errorCode: parsed.errorCode ?? 'SERVER_ERROR',
              errors: parsed.messages,
            );
          default:
            return ApiException(
              message: parsed.message,
              statusCode: statusCode,
              errorCode: parsed.errorCode,
              errors: parsed.messages,
            );
        }

      case DioExceptionType.cancel:
        return null;

      case DioExceptionType.badCertificate:
        return const ApiException(
          message: 'Server SSL certificate is invalid.',
          errorCode: 'CERTIFICATE_ERROR',
        );

      case DioExceptionType.unknown:
        return ApiException(
          message: err.message ?? 'An unexpected error occurred.',
          errorCode: 'UNKNOWN',
        );
    }
  }

  /// Parses the backend's `{ data, errors:[{code,message,meta}], meta }` shape
  /// into a flat, user-friendly view. Also tolerates legacy shapes (plain strings).
  /// For validation failures, the backend emits `code = "VALIDATION_ERROR"` and
  /// field name inside `meta.field` — we surface that as `fieldErrors[field] = message`.
  _ParsedError _parseEnvelope(dynamic data) {
    if (data is String && data.isNotEmpty) {
      return _ParsedError(message: data);
    }

    if (data is! Map<String, dynamic>) {
      return const _ParsedError(message: 'Something went wrong.');
    }

    final raw = data;
    final errorsList = raw['errors'];
    final messages = <String>[];
    final fieldErrors = <String, String>{};
    String? firstCode;
    Map<String, dynamic>? firstMeta;

    if (errorsList is List) {
      for (final e in errorsList) {
        if (e is Map) {
          final code = (e['code'] as String?) ?? '';
          final msg = (e['message'] as String?) ?? '';
          final metaObj = e['meta'];
          if (firstCode == null && code.isNotEmpty) {
            firstCode = code;
            firstMeta = metaObj is Map<String, dynamic>
                ? metaObj
                : metaObj is Map
                    ? Map<String, dynamic>.from(metaObj)
                    : null;
          }
          if (msg.isNotEmpty) messages.add(msg);

          if (code == 'VALIDATION_ERROR' &&
              metaObj is Map &&
              metaObj['field'] is String &&
              msg.isNotEmpty) {
            fieldErrors[metaObj['field'] as String] = msg;
          }
        } else if (e is String && e.isNotEmpty) {
          messages.add(e);
        }
      }
    }

    final headline = (raw['message'] as String?) ??
        (messages.isNotEmpty ? messages.first : 'Request failed.');

    return _ParsedError(
      message: headline,
      errorCode: (raw['errorCode'] as String?) ?? firstCode,
      messages: messages,
      fieldErrors: fieldErrors,
      firstMeta: firstMeta,
    );
  }
}

class _ParsedError {
  final String message;
  final String? errorCode;
  final List<String> messages;
  final Map<String, String> fieldErrors;
  final Map<String, dynamic>? firstMeta;

  const _ParsedError({
    required this.message,
    this.errorCode,
    this.messages = const [],
    this.fieldErrors = const {},
    this.firstMeta,
  });
}
