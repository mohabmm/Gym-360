import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/features/members/presentation/providers/members_provider.dart';

void main() {
  group('MemberFilter.withStatus', () {
    test('null clears an active status filter (the "All" chip)', () {
      const frozen = MemberFilter(
        search: 'sara',
        status: AppConstants.memberFrozen,
        page: 3,
      );

      final all = frozen.withStatus(null);

      expect(all.status, isNull);
      expect(all.search, 'sara');
      expect(all.page, 1);
    });

    test('switching status keeps the search text', () {
      const filter = MemberFilter(search: '0002');
      final active = filter.withStatus(AppConstants.memberActive);
      expect(active.status, AppConstants.memberActive);
      expect(active.search, '0002');
    });
  });
}
