import 'package:dio/dio.dart';
import 'package:logger/logger.dart';

import '../../services/telemetry_service.dart';

/// Logs HTTP request/response details to the console (debug) and forwards
/// failed calls to [TelemetryService] (→ SigNoz). Only method/path/status/error
/// metadata is shipped to telemetry — never headers or request/response bodies,
/// which may carry credentials or personal data (PDPL / Ironclad Rule #9).
class LoggingInterceptor extends Interceptor {
  LoggingInterceptor({
    TelemetryService? telemetry,
    this.consoleLogging = true,
  }) : _telemetry = telemetry;

  final TelemetryService? _telemetry;

  /// When false, console output is suppressed (release) but telemetry still flows.
  final bool consoleLogging;

  final Logger _logger = Logger(
    printer: PrettyPrinter(
      methodCount: 0,
      errorMethodCount: 5,
      lineLength: 80,
      colors: true,
      printEmojis: false,
      dateTimeFormat: DateTimeFormat.onlyTimeAndSinceStart,
    ),
  );

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (consoleLogging) {
      _logger.i(
        '${options.method} ${options.uri}\n'
        'Headers: ${_sanitizeHeaders(options.headers)}\n'
        '${options.data != null ? 'Body: ${options.data}' : ''}',
      );
    }
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (consoleLogging) {
      _logger.i(
        '${response.statusCode} ${response.requestOptions.method} '
        '${response.requestOptions.uri}\n'
        'Response: ${_truncate(response.data.toString(), 500)}',
      );
    }
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (consoleLogging) {
      _logger.e(
        '${err.response?.statusCode ?? "?"} ${err.requestOptions.method} '
        '${err.requestOptions.uri}\n'
        'Error: ${err.message}\n'
        'Response: ${err.response?.data}',
      );
    }

    // Forward to SigNoz. Cancellations are user-initiated, not failures.
    // Classify so real bugs are visible without drowning the error tab:
    //   • status == null (network/timeout/parse) or 5xx → error.
    //   • 4xx that signal a client/contract bug (400/408/409/422/429, etc.) →
    //     warning: queryable in SigNoz but kept out of the error tab.
    //   • 401/403/404 are expected app flow (auth refresh, not-found) → skip.
    //   • cancellations → skip (user-initiated).
    if (_telemetry != null && err.type != DioExceptionType.cancel) {
      final status = err.response?.statusCode;
      final level = _reportLevel(status);
      if (level != null) {
        _telemetry.recordApiError(
          method: err.requestOptions.method,
          path: err.requestOptions.path,
          statusCode: status,
          errorType: err.type.name,
          message: err.message,
          level: level,
        );
      }
    }

    handler.next(err);
  }

  /// Decides whether (and at what severity) a failed call is shipped to
  /// telemetry. Returns null to skip. See [onError] for the policy rationale.
  TelemetryLevel? _reportLevel(int? status) {
    if (status == null || status >= 500) return TelemetryLevel.error;
    // Expected app flow — auth refresh handles 401, 403/404 are routine.
    if (status == 401 || status == 403 || status == 404) return null;
    // Any other 4xx points at a client/contract bug worth surfacing.
    if (status >= 400) return TelemetryLevel.warning;
    return null; // 2xx/3xx never reach onError, but be explicit.
  }

  Map<String, dynamic> _sanitizeHeaders(Map<String, dynamic> headers) {
    final sanitized = Map<String, dynamic>.from(headers);
    if (sanitized.containsKey('Authorization')) {
      final auth = sanitized['Authorization'] as String?;
      if (auth != null && auth.length > 20) {
        sanitized['Authorization'] = '${auth.substring(0, 20)}...';
      }
    }
    return sanitized;
  }

  String _truncate(String text, int maxLength) {
    if (text.length <= maxLength) return text;
    return '${text.substring(0, maxLength)}...';
  }
}
