import 'dart:async';
import 'dart:isolate';

import 'package:flutter/foundation.dart';

import 'telemetry_service.dart';

/// Wraps app startup so that EVERY uncaught Dart error path is captured and
/// shipped via [telemetry]. There are four distinct entrypoints and a robust
/// setup must wire all of them — missing any one silently drops a whole class
/// of errors:
///   1. [FlutterError.onError]          — errors inside the widgets/framework.
///   2. [PlatformDispatcher.onError]    — errors outside a Flutter zone.
///   3. [runZonedGuarded]               — async errors in app code.
///   4. [Isolate.addErrorListener]      — errors on the root isolate.
///
/// Native (iOS/Android/platform-channel) crashes are NOT caught here — that is
/// deferred to Crashlytics in v1.5 (see plan).
///
/// Usage in each app's `main()`:
/// ```dart
/// Future<void> main() async {
///   await runWithTelemetry('gymapp-trainer', telemetry, () async {
///     WidgetsFlutterBinding.ensureInitialized();
///     // ... existing setup ...
///     runApp(const ProviderScope(child: GymAppTrainer()));
///   });
/// }
/// ```
Future<void> runWithTelemetry(
  String appName,
  TelemetryService telemetry,
  FutureOr<void> Function() body,
) async {
  // 1. Flutter framework errors.
  final previousOnError = FlutterError.onError;
  FlutterError.onError = (FlutterErrorDetails details) {
    telemetry.recordError(
      details.exception,
      details.stack,
      context: details.context?.toDescription(),
      level: details.silent ? TelemetryLevel.warning : TelemetryLevel.error,
    );
    if (kDebugMode) {
      // Keep the red screen / console dump in debug.
      previousOnError?.call(details);
    }
  };

  // 2. Errors that escape the Flutter zone (engine/platform callbacks).
  PlatformDispatcher.instance.onError = (Object error, StackTrace stack) {
    telemetry.recordError(error, stack,
        context: 'PlatformDispatcher', level: TelemetryLevel.fatal);
    return true; // handled — don't crash the engine.
  };

  // 4. Root-isolate errors (e.g. errors in spawned compute callbacks bubbling up).
  Isolate.current.addErrorListener(RawReceivePort((dynamic pair) {
    final List<dynamic> errorAndStack = pair as List<dynamic>;
    telemetry.recordError(
      errorAndStack.first ?? 'Isolate error',
      errorAndStack.length > 1 && errorAndStack[1] != null
          ? StackTrace.fromString(errorAndStack[1].toString())
          : null,
      context: 'Isolate',
      level: TelemetryLevel.fatal,
    );
  }).sendPort);

  // 3. runZonedGuarded wraps everything else (sync + async app code).
  await runZonedGuarded(() async {
    await body();
  }, (Object error, StackTrace stack) {
    telemetry.recordError(error, stack, context: 'Zone');
  });
}
