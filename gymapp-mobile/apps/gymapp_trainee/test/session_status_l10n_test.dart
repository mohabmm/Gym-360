import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/session_status_l10n.dart';

void main() {
  group('sessionStatusLabel — BE PascalCase compound enums', () {
    // Regression: these compound statuses previously fell through to the
    // default branch and rendered raw English inside the Arabic-first UI.
    test('CancelledByMember -> Arabic', () {
      expect(sessionStatusLabel('CancelledByMember', true), 'ألغاه المتدرب');
      expect(sessionStatusLabel('CancelledByMember', false),
          'Cancelled by trainee');
    });

    test('CancelledByTrainer -> Arabic', () {
      expect(sessionStatusLabel('CancelledByTrainer', true), 'ألغاه المدرب');
    });

    test('LateCancelled -> Arabic', () {
      expect(sessionStatusLabel('LateCancelled', true), 'إلغاء متأخر');
    });

    test('NoShow -> Arabic', () {
      expect(sessionStatusLabel('NoShow', true), 'لم يحضر');
    });

    test('InProgress -> Arabic', () {
      expect(sessionStatusLabel('InProgress', true), 'جارية');
    });

    test('Pending / Confirmed / Completed', () {
      expect(sessionStatusLabel('Pending', true), 'قيد الانتظار');
      expect(sessionStatusLabel('Confirmed', true), 'مؤكد');
      expect(sessionStatusLabel('Completed', true), 'مكتمل');
    });

    test('separators and casing are normalized', () {
      expect(sessionStatusLabel('late_cancelled', true), 'إلغاء متأخر');
      expect(sessionStatusLabel('NO_SHOW', true), 'لم يحضر');
      expect(sessionStatusLabel('cancelledbymember', true), 'ألغاه المتدرب');
    });

    test('unknown status falls back to the raw value', () {
      expect(sessionStatusLabel('SomethingNew', true), 'SomethingNew');
    });
  });

  group('sessionStatusColor', () {
    test('cancelled family is red', () {
      expect(sessionStatusColor('CancelledByMember'), Colors.red);
      expect(sessionStatusColor('CancelledByTrainer'), Colors.red);
    });
    test('completed is green, noshow grey, default blue', () {
      expect(sessionStatusColor('Completed'), Colors.green);
      expect(sessionStatusColor('NoShow'), Colors.grey);
      expect(sessionStatusColor('Pending'), Colors.blue);
    });
  });
}
