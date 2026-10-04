import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/core/session_type_l10n.dart';

void main() {
  group('sessionTypeLabel (trainer)', () {
    test('BE PascalCase enums -> Arabic', () {
      expect(sessionTypeLabel('PersonalTraining', true), 'تدريب شخصي');
      expect(sessionTypeLabel('Assessment', true), 'تقييم');
      expect(sessionTypeLabel('FollowUp', true), 'متابعة');
    });
    test('English locale', () {
      expect(sessionTypeLabel('Assessment', false), 'Assessment');
    });
    test('unknown falls back to raw', () {
      expect(sessionTypeLabel('Mystery', true), 'Mystery');
    });
  });
}
