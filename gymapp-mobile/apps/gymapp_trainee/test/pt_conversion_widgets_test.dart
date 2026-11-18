import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/providers.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/widgets/pt_conversion_widgets.dart';

class _FakeFlags implements FeatureFlagService {
  final FeatureFlags flags;
  _FakeFlags(this.flags);

  @override
  FeatureFlags get current => flags;
  @override
  Stream<FeatureFlags> get changes => const Stream.empty();
  @override
  Future<FeatureFlags> load() async => flags;
  @override
  Future<FeatureFlags> refresh() async => flags;
  @override
  Future<void> clear() async {}
  @override
  void dispose() {}
}

FeatureFlags _ptWithout(String key) => FeatureFlags(
      values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: k != key},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );

final _recap = TrialRecapModel(
  packageId: 'p1',
  trainerId: 't1',
  trainerName: 'Coach Omar',
  note: 'Strong start. Focus on form, 3x a week.',
  recommendedSessions: 12,
  sentAt: DateTime(2026, 11, 13),
  trialFinished: true,
);

/// The interest API: `/mine` answers [pending]; a POST records the call and makes it pending.
class _FakeInterestApi implements ApiClient {
  bool pending;
  int posts = 0;
  _FakeInterestApi({this.pending = false});

  @override
  Future<T> get<T>(String path,
      {Map<String, dynamic>? queryParameters, required T Function(dynamic data) parser, dynamic cancelToken}) async {
    expect(path, ApiEndpoints.packageInterestMine);
    return parser({'pending': pending, 'createdAt': pending ? '2026-11-18T09:00:00Z' : null});
  }

  @override
  Future<T> post<T>(String path,
      {dynamic data, Map<String, dynamic>? queryParameters, required T Function(dynamic data) parser, dynamic cancelToken}) async {
    posts++;
    pending = true;
    return parser({'interestId': 'i1', 'alreadyPending': false});
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

Widget _host(FeatureFlags flags, Widget child, {Locale locale = const Locale('en'), _FakeInterestApi? api}) =>
    ProviderScope(
      overrides: [
        featureFlagServiceProvider.overrideWithValue(_FakeFlags(flags)),
        apiClientProvider.overrideWithValue(api ?? _FakeInterestApi()),
      ],
      child: MaterialApp(
        locale: locale,
        supportedLocales: const [Locale('en'), Locale('ar')],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        home: Scaffold(body: SingleChildScrollView(child: child)),
      ),
    );

void main() {
  testWidgets('plan card shows the coach note, size and the interest button', (tester) async {
    await tester.pumpWidget(_host(FeatureFlags.ptDefaults(), TrialPlanCard(recap: _recap)));
    await tester.pumpAndSettle();

    expect(find.text('Your plan from Coach Omar'), findsOneWidget);
    expect(find.text('Strong start. Focus on form, 3x a week.'), findsOneWidget);
    expect(find.text('Recommended: 12 sessions'), findsOneWidget);
    expect(find.text("I'm interested"), findsOneWidget);
  });

  testWidgets('interest button is hidden while pt.package_interest is off', (tester) async {
    await tester.pumpWidget(_host(_ptWithout(FeatureFlagKeys.ptPackageInterest), TrialPlanCard(recap: _recap)));

    expect(find.text('Recommended: 12 sessions'), findsOneWidget);
    expect(find.text("I'm interested"), findsNothing);
  });

  testWidgets('Arabic plan card renders right-to-left', (tester) async {
    await tester.pumpWidget(_host(FeatureFlags.ptDefaults(), TrialPlanCard(recap: _recap), locale: const Locale('ar')));
    await tester.pumpAndSettle();

    expect(find.text('خطتك من Coach Omar'), findsOneWidget);
    expect(find.text('الباقة المقترحة: 12 حصة'), findsOneWidget);
    expect(find.text('مهتم بالباقة'), findsOneWidget);
    expect(Directionality.of(tester.element(find.byType(TrialPlanCard))), TextDirection.rtl);
  });

  testWidgets('a pending request shows "Request sent" instead of the button (e.g. after a restart)', (tester) async {
    await tester.pumpWidget(_host(FeatureFlags.ptDefaults(), TrialPlanCard(recap: _recap), api: _FakeInterestApi(pending: true)));
    await tester.pumpAndSettle();

    expect(find.text("I'm interested"), findsNothing);
    expect(find.textContaining('Request sent'), findsOneWidget);
  });

  testWidgets('one tap turns every interest/renew button into the sent request', (tester) async {
    final api = _FakeInterestApi();
    await tester.pumpWidget(_host(
      FeatureFlags.ptDefaults(),
      const Column(children: [
        InterestButton(packageId: 'p1', label: 'Renew with Omar'),
        InterestButton(packageId: 'p1', label: "I'm interested"),
      ]),
      api: api,
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Renew with Omar'));
    await tester.pumpAndSettle();

    expect(api.posts, 1);
    expect(find.text('Renew with Omar'), findsNothing);
    expect(find.text("I'm interested"), findsNothing);
    expect(find.textContaining('Request sent'), findsNWidgets(2));
  });
}
