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

  static const _timeoutTypes = {
    DioExceptionType.connectionTimeout,
    DioExceptionType.sendTimeout,
    DioExceptionType.receiveTimeout,
  };

  static const _serverErrorStatuses = {500, 502, 503, 504};

  ApiException? _mapToApiException(DioException err) {
    if (_timeoutTypes.contains(err.type)) {
      return const ApiException(
        message: 'The server is taking too long to respond. Please try again.',
        statusCode: 408,
        errorCode: 'TIMEOUT',
      );
    }

    switch (err.type) {
      case DioExceptionType.connectionError:
        return ApiException(
          message:
              'Could not reach the server. Check your internet connection '
              'or that the API host (${err.requestOptions.baseUrl}) is correct.',
          errorCode: 'NETWORK_ERROR',
        );

      case DioExceptionType.badResponse:
        return _mapBadResponse(err);

      case DioExceptionType.cancel:
        return null;

      case DioExceptionType.badCertificate:
        return const ApiException(
          message: 'Server SSL certificate is invalid.',
          errorCode: 'CERTIFICATE_ERROR',
        );

      // `default` rather than `case unknown`: newer dio releases add enum
      // values (e.g. transformTimeout in 5.10+) and an exhaustive switch stops
      // compiling against them.
      default:
        return ApiException(
          message: err.message ?? 'An unexpected error occurred.',
          errorCode: 'UNKNOWN',
        );
    }
  }

  ApiException _mapBadResponse(DioException err) {
    final response = err.response;
    if (response == null) {
      return ApiException(
        message: err.message ?? 'Empty response from server.',
        errorCode: 'NO_RESPONSE',
      );
    }

    final parsed = _parseEnvelope(response.data);
    return _mapBusinessError(parsed) ??
        _mapStatusCode(response.statusCode ?? 500, parsed);
  }

  /// Plan-gating and suspension errors are keyed by error code, not status.
  ApiException? _mapBusinessError(_ParsedError parsed) {
    final meta = parsed.firstMeta;
    switch (parsed.errorCode) {
      case 'PLAN.FEATURE_NOT_AVAILABLE':
        return FeatureGatedException(
          featureCode: (meta?['feature'] as String?) ?? '',
          requiredPlan: (meta?['requiredPlan'] as String?) ?? '',
        );
      case 'PLAN.STORAGE_LIMIT_REACHED':
      case 'PLAN.LIMIT_EXCEEDED':
        return StorageLimitException(
          currentCount: (meta?['currentUsage'] as int?) ?? 0,
          maxCount: (meta?['limit'] as num?)?.toInt() ?? 0,
          resourceType: (meta?['resource'] as String?) ?? 'items',
        );
      case 'GYM_SUSPENDED':
        return const GymSuspendedException();
      default:
        return null;
    }
  }

  ApiException _mapStatusCode(int statusCode, _ParsedError parsed) {
    if (_serverErrorStatuses.contains(statusCode)) {
      return ApiException(
        message: parsed.message.isEmpty
            ? 'Server error. Please try again shortly.'
            : parsed.message,
        statusCode: statusCode,
        errorCode: parsed.errorCode ?? 'SERVER_ERROR',
        errors: parsed.messages,
      );
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
      default:
        return ApiException(
          message: parsed.message,
          statusCode: statusCode,
          errorCode: parsed.errorCode,
          errors: parsed.messages,
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

    final errors = _EnvelopeErrors()..addAll(data['errors']);
    final messages = errors.messages;
    final headline = (data['message'] as String?) ??
        (messages.isNotEmpty ? messages.first : 'Request failed.');

    return _ParsedError(
      message: headline,
      errorCode: (data['errorCode'] as String?) ?? errors.firstCode,
      messages: messages,
      fieldErrors: errors.fieldErrors,
      firstMeta: errors.firstMeta,
    );
  }
}

/// Accumulates the entries of the envelope's `errors` list.
class _EnvelopeErrors {
  final List<String> messages = [];
  final Map<String, String> fieldErrors = {};
  String? firstCode;
  Map<String, dynamic>? firstMeta;

  void addAll(Object? errorsList) {
    if (errorsList is! List) return;
    for (final e in errorsList) {
      if (e is Map) {
        _addStructured(e);
      } else if (e is String && e.isNotEmpty) {
        messages.add(e);
      }
    }
  }

  void _addStructured(Map<dynamic, dynamic> e) {
    final code = (e['code'] as String?) ?? '';
    final msg = (e['message'] as String?) ?? '';
    final metaObj = e['meta'];
    if (firstCode == null && code.isNotEmpty) {
      firstCode = code;
      firstMeta = _asJsonMap(metaObj);
    }
    if (msg.isEmpty) return;
    messages.add(msg);

    final field =
        code == 'VALIDATION_ERROR' && metaObj is Map ? metaObj['field'] : null;
    if (field is String) fieldErrors[field] = msg;
  }

  static Map<String, dynamic>? _asJsonMap(Object? value) {
    if (value is Map<String, dynamic>) return value;
    return value is Map ? Map<String, dynamic>.from(value) : null;
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
