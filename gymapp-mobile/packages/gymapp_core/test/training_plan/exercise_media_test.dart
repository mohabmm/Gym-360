import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  test('a demo path from the API is served from the API host; a coach link stays as it is', () {
    expect(exerciseMediaUrl('/media/exercises/plank.webp', baseUrl: 'https://api.example.com/'),
        'https://api.example.com/media/exercises/plank.webp');
    expect(exerciseMediaUrl('https://youtu.be/abc', baseUrl: 'https://api.example.com'), 'https://youtu.be/abc');
    expect(exerciseMediaUrl(null), isNull);
    expect(exerciseMediaUrl('  '), isNull);
  });

  test('the member plan carries each library exercise demo by exercise key', () {
    final plan = MyPlanModel.fromJson({
      'plan': {'summary': {'id': 'p', 'startDate': '2026-11-23', 'updatedAt': '2026-11-23T00:00:00Z'}, 'document': {}},
      'cursor': {},
      'stats': {},
      'exerciseMedia': {'e1': '/media/exercises/plank.webp'},
    });

    expect(plan.exerciseMedia, {'e1': '/media/exercises/plank.webp'});
    expect(MyPlanModel.fromJson({'plan': {'summary': {'id': 'p'}}, 'cursor': {}, 'stats': {}}).exerciseMedia, isEmpty,
        reason: 'an older API without media still parses');
  });
}
