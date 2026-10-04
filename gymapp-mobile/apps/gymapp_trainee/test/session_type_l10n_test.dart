import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/session_status_l10n.dart';

void main() {
  group('sessionTypeLabel (trainee)', () {
    test('BE PascalCase enums -> Arabic', () {
      expect(sessionTypeLabel('PersonalTraining', true), 'تدريب شخصي');
      expect(sessionTypeLabel('Assessment', true), 'تقييم');
      expect(sessionTypeLabel('FollowUp', true), 'متابعة');
    });
    test('English locale', () {
      expect(sessionTypeLabel('PersonalTraining', false), 'Personal Training');
      expect(sessionTypeLabel('FollowUp', false), 'Follow-up');
    });
    test('normalizes case/separators', () {
      expect(sessionTypeLabel('personal_training', true), 'تدريب شخصي');
      expect(sessionTypeLabel('follow-up', true), 'متابعة');
    });
    test('unknown falls back to raw', () {
      expect(sessionTypeLabel('Mystery', true), 'Mystery');
    });
  });
}
