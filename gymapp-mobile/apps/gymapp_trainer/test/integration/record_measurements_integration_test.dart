import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/screens/record_measurements_screen.dart';

import 'helpers/test_api_client.dart';

/// pt.progress_measurements: the coach's check-in screen against the real API client stack —
/// what goes over the wire (date, tape incl. calves/neck, cm) and how a 409 is surfaced.
late TestApiClient t;

Future<void> _pumpScreen(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1200, 4000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  t.adapter.onGet(
    ApiEndpoints.clientProgressJourney(kClient),
    respond: (_) => t.adapter.ok({'userId': kClientId, 'entries': [], 'goal': {'goal': 'LoseFat'}}),
  );
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      secureStorageProvider.overrideWithValue(t.storage),
    ],
    child: MaterialApp(
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      // Client detail → Measurements, as in the app.
      home: Builder(
        builder: (context) => Scaffold(
          body: TextButton(
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute<void>(builder: (_) => const RecordMeasurementsScreen(clientId: kClientId)),
            ),
            child: const Text('client detail'),
          ),
        ),
      ),
    ),
  ));
  await tester.tap(find.text('client detail'));
  await tester.pumpAndSettle();
}

Future<void> _fill(WidgetTester tester, Map<ProgressMetric, String> values) async {
  for (final e in values.entries) {
    await tester.enterText(find.byKey(ValueKey('metric-${e.key.name}')), e.value);
  }
  await tester.pump();
}

void main() {
  setUp(() => t = buildTestApiClient());
  _savesTheCheckIn();
  _surfacesConflicts();
}

void _savesTheCheckIn() {
  testWidgets('POST body-metrics/client sends date, weight, body fat and the full tape in cm', (tester) async {
    t.adapter.onPost(ApiEndpoints.bodyMetricsForClient, respond: (_) => t.adapter.created('entry-1'));
    await _pumpScreen(tester);

    await _fill(tester, {
      ProgressMetric.weight: '82,4',
      ProgressMetric.bodyFat: '18',
      ProgressMetric.neck: '39',
      ProgressMetric.leftCalf: '37.5',
      ProgressMetric.rightCalf: '38',
    });
    await tester.tap(find.byKey(const ValueKey('save-measurements')));
    await tester.pumpAndSettle();

    final body = t.requestWhere(method: 'POST', path: ApiEndpoints.bodyMetricsForClient).bodyAsMap!;
    expect(body['clientId'], kClientId);
    expect(body['gymId'], kGymId);
    expect(body['date'], apiDate(DateTime.now()));
    expect(body['weight'], 82.4);
    expect(body['weightUnit'], 'kg');
    expect(body['bodyFatPercentage'], 18.0);
    final tape = body['measurements'] as Map<String, dynamic>;
    expect(tape['neck'], 39.0);
    expect(tape['leftCalf'], 37.5);
    expect(tape['rightCalf'], 38.0);
    expect(tape['waist'], isNull);
    expect(tape['measurementUnit'], 'cm');
    expect(find.text('Measurements saved'), findsOneWidget);
    expect(find.text('client detail'), findsOneWidget, reason: 'returns to the client after saving');
  });

  testWidgets('weight only: no measurements object is sent', (tester) async {
    t.adapter.onPost(ApiEndpoints.bodyMetricsForClient, respond: (_) => t.adapter.created('entry-1'));
    await _pumpScreen(tester);

    await _fill(tester, {ProgressMetric.weight: '80'});
    await tester.tap(find.byKey(const ValueKey('save-measurements')));
    await tester.pumpAndSettle();

    final body = t.requestWhere(method: 'POST', path: ApiEndpoints.bodyMetricsForClient).bodyAsMap!;
    expect(body['measurements'], isNull);
    expect(body['notes'], isNull);
  });
}

void _surfacesConflicts() {
  testWidgets('409 duplicate keeps the coach on the form with a clear message', (tester) async {
    t.adapter.onPost(
      ApiEndpoints.bodyMetricsForClient,
      respond: (_) => t.adapter.rawResponse(409, {
        'success': false,
        'error': {'code': 'Progress.DuplicateMetricEntry', 'message': 'Duplicate'},
      }),
    );
    await _pumpScreen(tester);

    await _fill(tester, {ProgressMetric.weight: '80'});
    await tester.tap(find.byKey(const ValueKey('save-measurements')));
    await tester.pumpAndSettle();

    expect(find.text('A check-in already exists on this day. Pick another date.'), findsOneWidget);
    expect(find.byKey(const ValueKey('metric-weight')), findsOneWidget);
  });
}
