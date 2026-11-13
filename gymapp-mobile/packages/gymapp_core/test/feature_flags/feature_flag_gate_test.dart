import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

class _FakeService implements FeatureFlagService {
  FeatureFlags flags;
  int refreshes = 0;
  _FakeService(this.flags);

  @override
  FeatureFlags get current => flags;
  @override
  Stream<FeatureFlags> get changes => const Stream.empty();
  @override
  Future<FeatureFlags> load() async => flags;
  @override
  Future<FeatureFlags> refresh() async {
    refreshes++;
    return flags;
  }

  @override
  Future<void> clear() async {}
  @override
  void dispose() {}
}

void main() {
  Widget host(_FakeService service, Widget child) => ProviderScope(
        overrides: [featureFlagServiceProvider.overrideWithValue(service)],
        child: MaterialApp(home: Scaffold(body: Column(children: [child, const Text('below')]))),
      );

  testWidgets('disabled feature renders nothing (no gap)', (tester) async {
    await tester.pumpWidget(host(
      _FakeService(FeatureFlags.ptDefaults()),
      const FeatureFlagGate(flag: FeatureFlagKeys.nutrition, child: SizedBox(height: 120, child: Text('nutrition'))),
    ));

    expect(find.text('nutrition'), findsNothing);
    expect(tester.getTopLeft(find.text('below')).dy, 0);
  });

  testWidgets('enabled feature renders its child', (tester) async {
    await tester.pumpWidget(host(
      _FakeService(FeatureFlags.ptDefaults()),
      const FeatureFlagGate(flag: FeatureFlagKeys.ptPackages, child: Text('packages')),
    ));

    expect(find.text('packages'), findsOneWidget);
  });

  testWidgets('FEATURE_DISABLED from the API triggers a flags refresh', (tester) async {
    final service = _FakeService(FeatureFlags.ptDefaults());
    await tester.pumpWidget(host(service, Consumer(builder: (_, ref, __) {
      ref.watch(featureFlagsProvider);
      return const SizedBox();
    })));

    FeatureFlagSignals.reportDisabled(FeatureFlagKeys.nutrition);
    await tester.pump();

    expect(service.refreshes, 1);
  });
}
