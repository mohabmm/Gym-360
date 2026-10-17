import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// Locks in the Bug #2 fix: AppErrorWidget must never leak the raw
/// `ApiException(N): …` toString to end users. Status-aware friendly text
/// in both English and Arabic locales.
Future<void> _pump(
  WidgetTester tester,
  AppErrorWidget child, {
  Locale locale = const Locale('en'),
}) async {
  await tester.pumpWidget(
    MaterialApp(
      locale: locale,
      supportedLocales: const [Locale('en'), Locale('ar')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: Scaffold(body: child),
    ),
  );
}

void main() {
  group('AppErrorWidget — friendly-message resolver', () {
    _fallbackMessageTests();
    _passThroughMessageTests();
  });
}

/// Raw `ApiException(N)` text is replaced by status-aware localised copy.
void _fallbackMessageTests() {
  testWidgets('replaces ApiException(405) generic message with English fallback',
      (tester) async {
    await _pump(
      tester,
      const AppErrorWidget(
        message: '.ApiException(405): Something went wrong',
      ),
    );
    expect(find.text(".ApiException(405): Something went wrong"),
        findsNothing);
    expect(find.text("This action isn't supported here. Please update the app."),
        findsOneWidget);
  });

  testWidgets('replaces ApiException(405) with Arabic fallback when locale=ar',
      (tester) async {
    await _pump(
      tester,
      const AppErrorWidget(
        message: 'ApiException(405): Something went wrong.',
      ),
      locale: const Locale('ar'),
    );
    expect(find.textContaining('ApiException'), findsNothing);
    expect(find.text('هذا الإجراء غير مدعوم. يرجى تحديث التطبيق.'),
        findsOneWidget);
  });

  testWidgets('maps known status codes to localised text', (tester) async {
    const cases = <int, String>{
      401: 'Please sign in again.',
      403: "You don't have permission to view this.",
      404: 'Not found.',
      500: 'Server error. Please try again shortly.',
    };
    for (final entry in cases.entries) {
      await _pump(
        tester,
        AppErrorWidget(
          message: 'ApiException(${entry.key}): Something went wrong',
        ),
      );
      expect(find.text(entry.value), findsOneWidget,
          reason: 'status ${entry.key} should map to "${entry.value}"');
    }
  });
}

/// Useful or non-ApiException messages are shown as-is (or generically).
void _passThroughMessageTests() {
  testWidgets('preserves a useful inner message instead of falling back',
      (tester) async {
    await _pump(
      tester,
      const AppErrorWidget(
        message:
            'ApiException(400): The member was not found in this gym.',
      ),
    );
    // Useful, specific server messages should pass through verbatim.
    expect(find.text('The member was not found in this gym.'),
        findsOneWidget);
    // The exception class name must not appear.
    expect(find.textContaining('ApiException('), findsNothing);
  });

  testWidgets('passes plain (non-ApiException) messages through unchanged',
      (tester) async {
    await _pump(
      tester,
      const AppErrorWidget(message: 'Disk full'),
    );
    expect(find.text('Disk full'), findsOneWidget);
  });

  testWidgets('null status code falls back to generic message',
      (tester) async {
    await _pump(
      tester,
      const AppErrorWidget(
        message: 'ApiException(null): Something went wrong',
      ),
    );
    expect(find.text('Something went wrong.'), findsOneWidget);
  });
}
