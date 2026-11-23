import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/feature_flags.dart';
import 'package:gymapp_gym/core/providers.dart';
import 'package:gymapp_gym/core/router.dart';
import 'package:gymapp_gym/features/pt/presentation/screens/challenge_awards_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../integration/helpers/test_api_client.dart';

Map<String, dynamic> _award({String? deliveredAt}) => {
      'awardId': 'a1',
      'traineeId': 't1',
      'traineeName': 'أحمد علي',
      'trainerId': 'c1',
      'trainerName': 'كابتن محمود',
      'count': 12,
      'target': 12,
      'windowStart': '2026-11-01',
      'windowEnd': '2026-11-30',
      'reachedAt': '2026-11-20T10:00:00Z',
      'rewardTextAr': 'جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي',
      'rewardTextEn': 'A follow-up session with your coach',
      'deliveredAt': deliveredAt,
    };

void main() {
  setUpAll(() async => initializeDateFormatting('ar'));

  testWidgets('the desk sees who earned the reward and ticks it delivered', (tester) async {
    final api = buildTestApiClient();
    var delivered = false;
    api.adapter.onGet(ApiEndpoints.challengeAwards, respond: (request) {
      final wantsDelivered = request.query['delivered']?.first == 'true';
      return api.adapter.ok(wantsDelivered == delivered ? [_award(deliveredAt: delivered ? '2026-11-21T10:00:00Z' : null)] : []);
    });
    api.adapter.onPut(ApiEndpoints.challengeAwardDelivered('a1'), respond: (_) {
      delivered = true;
      return api.adapter.ok(_award(deliveredAt: '2026-11-21T10:00:00Z'));
    });

    await tester.pumpWidget(ProviderScope(
      overrides: [apiClientProvider.overrideWithValue(api.apiClient)],
      child: const MaterialApp(
        locale: Locale('ar'),
        supportedLocales: [Locale('ar'), Locale('en')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        home: ChallengeAwardsScreen(),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('أحمد علي'), findsOneWidget);
    expect(find.textContaining('12/12 تمرين'), findsOneWidget);
    expect(find.textContaining('الكابتن كابتن محمود'), findsOneWidget);
    expect(find.textContaining('جلسة متابعة مع المدرب'), findsOneWidget);

    await tester.tap(find.byType(Checkbox));
    // The mocked network answers without scheduling a frame: pump until it has.
    for (var i = 0; i < 5; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
    await tester.pumpAndSettle();

    final put = api.adapter.recorded.singleWhere((r) => r.method == 'PUT');
    expect(put.bodyAsMap, {'delivered': true});
    expect(find.text('أحمد علي'), findsNothing, reason: 'it moved to the Delivered tab');
    expect(find.text('مفيش مكافآت مستنية'), findsOneWidget);
  });

  test('the rewards list is closed unless mem.first_month_challenge is on', () {
    FeatureFlags flags(bool challenge) => FeatureFlags(
          values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: true, FeatureFlagKeys.memFirstMonthChallenge: challenge},
          preset: 'pt_module',
          origin: FeatureFlagsOrigin.server,
        );
    expect(staffRouteGuard.redirect(RoutePaths.ptChallenge, flags(false)), RoutePaths.dashboard);
    expect(staffRouteGuard.redirect(RoutePaths.ptChallenge, flags(true)), isNull);
  });
}
