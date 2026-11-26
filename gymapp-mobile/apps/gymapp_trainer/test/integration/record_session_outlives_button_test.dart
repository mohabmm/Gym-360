import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/providers/client_providers.dart';
import 'package:gymapp_trainer/features/revenue/presentation/providers/revenue_providers.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/pt_coach_widgets.dart';

import 'helpers/test_api_client.dart';

/// Staging crash (SigNoz, 26 Nov 2026): the client list behind the open "Record session" sheet
/// reloaded (token refresh), the "Log session" button was unmounted, and the next sheet rebuild
/// threw "Bad state: No ProviderScope found" — the coach could not record the session.
void main() {
  late TestApiClient t;
  setUp(() => t = buildTestApiClient());

  testWidgets('the sheet keeps working and still records after the button behind it is gone', (tester) async {
    var packageCalls = 0;
    t.adapter.onPost(ApiEndpoints.sessionQuickLog,
        respond: (_) => t.adapter.ok({
              'sessionId': 'sess-1',
              'remainingSessions': 7,
              'alreadyLogged': false,
              'date': '2026-11-20',
              'startTime': '18:30:00',
            }));
    tester.view.physicalSize = const Size(1200, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final showButton = ValueNotifier(true);

    await tester.pumpWidget(ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(t.apiClient),
        clockProvider.overrideWithValue(() => DateTime(2026, 11, 20, 18, 40)),
        allClientPackagesProvider.overrideWith((ref) async {
          packageCalls++;
          return const [
            SessionPackageModel(
                id: 'pkg-1', memberId: kClientId, memberName: 'Mona Ali', totalSessions: 12, remainingSessions: 8),
          ];
        }),
        clientNameByIdProvider.overrideWithValue(const {}),
        isFeatureEnabledProvider.overrideWith((ref, flag) => true),
      ],
      child: MaterialApp.router(
        routerConfig: GoRouter(routes: [
          GoRoute(
            path: '/',
            builder: (_, __) => Scaffold(
              body: ValueListenableBuilder<bool>(
                valueListenable: showButton,
                builder: (_, show, __) =>
                    show ? const QuickLogButton(memberId: kClientId) : const Text('list reloading'),
              ),
            ),
          ),
        ]),
        locale: const Locale('en'),
        supportedLocales: const [Locale('ar'), Locale('en')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
      ),
    ));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('log-session')));
    await tester.pumpAndSettle();
    final callsBefore = packageCalls;

    showButton.value = false;
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).first, 'focused on back');
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(const ValueKey('record-session')));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(t.adapter.recorded.where((r) => r.method == 'POST'), hasLength(1));
    showButton.value = true;
    await tester.pumpAndSettle();
    expect(packageCalls, greaterThan(callsBefore), reason: 'the sessions left are reloaded after recording');
  });
}
