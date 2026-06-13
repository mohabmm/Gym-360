import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('Debouncer', () {
    test('fires the last callback after the delay', () async {
      final debouncer = Debouncer(milliseconds: 40);
      var counter = 0;

      debouncer.run(() => counter++);
      debouncer.run(() => counter++);
      debouncer.run(() => counter++);

      expect(counter, 0);
      await Future<void>.delayed(const Duration(milliseconds: 80));
      expect(counter, 1);
    });

    test('cancel prevents firing', () async {
      final debouncer = Debouncer(milliseconds: 30);
      var fired = false;

      debouncer.run(() => fired = true);
      debouncer.cancel();

      await Future<void>.delayed(const Duration(milliseconds: 80));
      expect(fired, isFalse);
    });

    test('dispose prevents firing', () async {
      final debouncer = Debouncer(milliseconds: 30);
      var fired = false;

      debouncer.run(() => fired = true);
      debouncer.dispose();

      await Future<void>.delayed(const Duration(milliseconds: 80));
      expect(fired, isFalse);
    });
  });
}
