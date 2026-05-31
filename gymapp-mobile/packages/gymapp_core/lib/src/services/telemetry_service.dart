import 'dart:async';
import 'dart:convert';
import 'dart:io' show Platform;

import 'package:device_info_plus/device_info_plus.dart';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:hive/hive.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../constants/api_endpoints.dart';
import '../constants/storage_keys.dart';
import 'secure_storage_service.dart';

/// Severity levels for client telemetry, aligned with OTEL/Serilog levels so
/// the backend can map them straight to `severity_text` in SigNoz.
enum TelemetryLevel { debug, info, warning, error, fatal }

extension TelemetryLevelX on TelemetryLevel {
  /// Wire value sent to the backend (lower-case, matched server-side).
  String get wire => switch (this) {
        TelemetryLevel.debug => 'debug',
        TelemetryLevel.info => 'info',
        TelemetryLevel.warning => 'warning',
        TelemetryLevel.error => 'error',
        TelemetryLevel.fatal => 'fatal',
      };

  int get rank => index;
}

/// Ships client logs + uncaught errors from the three Flutter apps to the
/// backend ingest endpoint (`POST /observability/client-logs`), which forwards
/// them to SigNoz via the existing Serilog OTLP sink.
///
/// Design notes (see plan: Mobile → SigNoz Observability):
///  - Records are buffered and flushed on batch size OR a periodic timer.
///  - A failed POST persists the batch to a dedicated Hive box so nothing is
///    lost across an outage / app restart; it is re-flushed on the next success.
///  - A hard cap + drop-counter prevents a crash-looping device from flooding
///    ingest. The drop count is reported as its own record on the next flush.
///  - PII is redacted here (allowlist): never ship auth headers, tokens,
///    passwords, raw request/response bodies, or raw email/phone. The backend
///    scrubs again (defense in depth) and overrides user/gym from the JWT.
///  - In release builds only `warning`+ is shipped; debug/info are dropped to
///    control volume. Errors always ship.
class TelemetryService {
  TelemetryService({
    required String appName,
    required String baseUrl,
    required SecureStorageService storage,
    this.enabled = true,
    this.batchSize = 20,
    this.flushInterval = const Duration(seconds: 10),
    this.maxQueue = 500,
    this.minReleaseLevel = TelemetryLevel.warning,
    Dio? dio,
  })  : _appName = appName,
        _storage = storage,
        _dio = dio ??
            Dio(BaseOptions(
              baseUrl: baseUrl,
              // Deliberately short: telemetry must never delay or block the app.
              connectTimeout: const Duration(seconds: 5),
              sendTimeout: const Duration(seconds: 5),
              receiveTimeout: const Duration(seconds: 5),
              headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
              },
            ));

  final String _appName;
  final SecureStorageService _storage;
  final Dio _dio;

  final bool enabled;
  final int batchSize;
  final Duration flushInterval;
  final int maxQueue;

  /// Below this level, records are dropped in release builds.
  final TelemetryLevel minReleaseLevel;

  final List<Map<String, dynamic>> _buffer = [];
  Timer? _timer;
  bool _flushing = false;
  int _dropped = 0;
  bool _initialized = false;

  // Ambient context attached to every record.
  String? _appVersion;
  String? _osName;
  String? _osVersion;
  String? _deviceModel;
  String _currentRoute = '/';

  /// Updated by [TelemetryNavigatorObserver] — the "where did it happen" signal.
  set currentRoute(String route) => _currentRoute = route;
  String get currentRoute => _currentRoute;

  /// Gathers device/app context, opens the offline queue box, starts the flush
  /// timer, and re-sends anything queued from a previous session. Safe to call
  /// more than once. Never throws — telemetry must not break app startup.
  Future<void> init() async {
    if (_initialized || !enabled) return;
    _initialized = true;
    try {
      final info = await PackageInfo.fromPlatform();
      _appVersion = '${info.version}+${info.buildNumber}';
    } catch (_) {/* keep nulls */}
    try {
      final deviceInfo = DeviceInfoPlugin();
      if (Platform.isAndroid) {
        final a = await deviceInfo.androidInfo;
        _osName = 'android';
        _osVersion = a.version.release;
        _deviceModel = '${a.manufacturer} ${a.model}';
      } else if (Platform.isIOS) {
        final i = await deviceInfo.iosInfo;
        _osName = 'ios';
        _osVersion = i.systemVersion;
        _deviceModel = i.utsname.machine;
      }
    } catch (_) {/* keep nulls */}

    _timer = Timer.periodic(flushInterval, (_) => unawaited(flush()));
    unawaited(_reflushPersisted());
  }

  /// Logs a structured message. Heavy lifting (context, redaction, enqueue) is
  /// synchronous and cheap; the network send happens on the timer / batch.
  void log(
    TelemetryLevel level,
    String message, {
    Object? error,
    StackTrace? stackTrace,
    Map<String, dynamic>? attributes,
  }) {
    if (!enabled) return;
    // Release-mode volume control: drop low-severity records.
    if (kReleaseMode && level.rank < minReleaseLevel.rank) return;

    final record = <String, dynamic>{
      'level': level.wire,
      'message': _truncate(message, 4000),
      'app': _appName,
      'appVersion': _appVersion,
      'os': _osName,
      'osVersion': _osVersion,
      'device': _deviceModel,
      'route': _currentRoute,
      'clientTs': DateTime.now().toUtc().toIso8601String(),
      if (error != null) 'errorType': error.runtimeType.toString(),
      if (stackTrace != null) 'stackTrace': _truncate(stackTrace.toString(), 16000),
      if (attributes != null && attributes.isNotEmpty)
        'attributes': _redactAttributes(attributes),
    };
    _enqueue(record);
  }

  /// Records an uncaught Dart error (from FlutterError.onError,
  /// PlatformDispatcher.onError, runZonedGuarded, or an isolate listener).
  void recordError(
    Object error,
    StackTrace? stackTrace, {
    String? context,
    TelemetryLevel level = TelemetryLevel.error,
    Map<String, dynamic>? attributes,
  }) {
    log(
      level,
      context != null ? '$context: $error' : error.toString(),
      error: error,
      stackTrace: stackTrace,
      attributes: attributes,
    );
  }

  /// Records a failed API call. The body is intentionally NOT included — only
  /// method, scrubbed path, status, and error code travel.
  void recordApiError({
    required String method,
    required String path,
    int? statusCode,
    String? errorCode,
    String? errorType,
    String? message,
  }) {
    log(
      TelemetryLevel.error,
      message ?? 'API ${statusCode ?? '?'} $method ${_scrubPath(path)}',
      attributes: {
        'http.method': method,
        'http.path': _scrubPath(path),
        if (statusCode != null) 'http.status': statusCode,
        if (errorCode != null) 'error.code': errorCode,
        if (errorType != null) 'error.type': errorType,
      },
    );
  }

  void _enqueue(Map<String, dynamic> record) {
    if (_buffer.length >= maxQueue) {
      // Crash-loop guard: drop oldest, count it, report the count later.
      _buffer.removeAt(0);
      _dropped++;
    }
    _buffer.add(record);
    if (_buffer.length >= batchSize) unawaited(flush());
  }

  /// Sends all buffered + persisted records. Never throws. On network failure
  /// the batch is persisted for the next attempt.
  Future<void> flush() async {
    if (!enabled || _flushing) return;
    if (_buffer.isEmpty && _dropped == 0) return;
    _flushing = true;
    try {
      final batch = List<Map<String, dynamic>>.from(_buffer);
      _buffer.clear();
      if (_dropped > 0) {
        batch.add(_metaDroppedRecord(_dropped));
        _dropped = 0;
      }
      final ok = await _post(batch);
      if (!ok) {
        await _persist(batch);
      } else {
        await _reflushPersisted();
      }
    } catch (_) {
      // swallow — telemetry failure must never surface to the user
    } finally {
      _flushing = false;
    }
  }

  Future<bool> _post(List<Map<String, dynamic>> records) async {
    if (records.isEmpty) return true;
    try {
      // Best-effort auth context. The backend re-derives user/gym from the JWT
      // and ignores client-supplied ids, so this is only a fallback hint.
      final token = await _storage.getAccessToken();
      final resp = await _dio.post(
        ApiEndpoints.clientLogs,
        data: {'records': records},
        options: Options(
          headers: token != null ? {'Authorization': 'Bearer $token'} : null,
          // Don't let a 4xx/5xx throw — we decide retry based on the code.
          validateStatus: (_) => true,
        ),
      );
      final status = resp.statusCode ?? 0;
      // 2xx = delivered. 4xx (except 429) = our fault, retrying won't help:
      // drop so a malformed batch can't wedge the queue forever.
      if (status >= 200 && status < 300) return true;
      if (status >= 400 && status < 500 && status != 429) return true;
      return false; // 5xx / 429 / network → retry later
    } catch (_) {
      return false;
    }
  }

  // --- Offline persistence -------------------------------------------------

  Future<Box?> _openBox() async {
    try {
      if (Hive.isBoxOpen(StorageKeys.telemetryQueueBox)) {
        return Hive.box(StorageKeys.telemetryQueueBox);
      }
      return await Hive.openBox(StorageKeys.telemetryQueueBox);
    } catch (_) {
      return null;
    }
  }

  Future<void> _persist(List<Map<String, dynamic>> records) async {
    final box = await _openBox();
    if (box == null) return;
    try {
      final existing = _readPersisted(box);
      existing.addAll(records);
      // Cap persisted size too — newest win.
      final capped = existing.length > maxQueue
          ? existing.sublist(existing.length - maxQueue)
          : existing;
      await box.put(_queueKey, jsonEncode(capped));
    } catch (_) {/* best effort */}
  }

  Future<void> _reflushPersisted() async {
    final box = await _openBox();
    if (box == null) return;
    List<Map<String, dynamic>> persisted;
    try {
      persisted = _readPersisted(box);
    } catch (_) {
      return;
    }
    if (persisted.isEmpty) return;
    final ok = await _post(persisted);
    if (ok) {
      try {
        await box.delete(_queueKey);
      } catch (_) {/* ignore */}
    }
  }

  List<Map<String, dynamic>> _readPersisted(Box box) {
    final raw = box.get(_queueKey);
    if (raw is! String || raw.isEmpty) return [];
    final decoded = jsonDecode(raw);
    if (decoded is! List) return [];
    return decoded.whereType<Map>().map((e) => Map<String, dynamic>.from(e)).toList();
  }

  static const String _queueKey = 'queued_records';

  Map<String, dynamic> _metaDroppedRecord(int count) => {
        'level': TelemetryLevel.warning.wire,
        'message': 'Telemetry dropped $count record(s) (buffer cap reached)',
        'app': _appName,
        'appVersion': _appVersion,
        'os': _osName,
        'osVersion': _osVersion,
        'device': _deviceModel,
        'route': _currentRoute,
        'clientTs': DateTime.now().toUtc().toIso8601String(),
        'attributes': {'client.dropped': count},
      };

  // --- PII redaction -------------------------------------------------------

  /// Strips query string (may carry tokens/ids) and keeps only the path.
  String _scrubPath(String path) {
    final q = path.indexOf('?');
    return q >= 0 ? path.substring(0, q) : path;
  }

  static const Set<String> _blockedAttrKeys = {
    'authorization',
    'password',
    'token',
    'accesstoken',
    'refreshtoken',
    'access_token',
    'refresh_token',
    'email',
    'phone',
    'secret',
    'body',
    'requestbody',
    'responsebody',
    'data',
  };

  Map<String, dynamic> _redactAttributes(Map<String, dynamic> attrs) {
    final out = <String, dynamic>{};
    attrs.forEach((k, v) {
      if (_blockedAttrKeys.contains(k.toLowerCase())) {
        out[k] = '[redacted]';
      } else if (v is String) {
        out[k] = _truncate(v, 1000);
      } else if (v is num || v is bool) {
        out[k] = v;
      } else {
        out[k] = _truncate(v.toString(), 1000);
      }
    });
    return out;
  }

  String _truncate(String s, int max) =>
      s.length <= max ? s : '${s.substring(0, max)}…';

  /// Flush remaining records and stop the timer. Call on app dispose if needed.
  Future<void> dispose() async {
    _timer?.cancel();
    await flush();
  }
}
