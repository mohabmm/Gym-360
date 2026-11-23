import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  final json = {
    'schemaVersion': 1,
    'weeks': [
      {
        'week': 2,
        'days': [
          {'key': 'd2', 'title': 'Day B', 'titleAr': 'اليوم ب', 'exercises': []}
        ]
      },
      {
        'week': 1,
        'days': [
          {
            'key': 'd1',
            'title': 'Day A',
            'exercises': [
              {'key': 'e1', 'name': 'Plank', 'nameAr': 'بلانك', 'sets': 3, 'seconds': 45, 'restSeconds': 30},
              {'key': 'e2', 'name': 'Squat', 'sets': 4, 'reps': '8-10', 'targetWeightKg': 60, 'rpe': 8},
            ]
          }
        ]
      },
    ],
  };

  test('the plan document reads schema v1 and lists days in week order', () {
    final doc = PlanDocumentModel.fromJson(json);

    expect(doc.sequence.map((d) => d.key), ['d1', 'd2']);
    expect(doc.daysPerWeek, 1);
    expect(doc.sequence.first.exercises.first.isTimed, isTrue);
    expect(doc.sequence.last.displayTitle(true), 'اليوم ب');
  });

  test('a document survives a round trip to JSON', () {
    final doc = PlanDocumentModel.fromJson(json);

    final again = PlanDocumentModel.fromJson(doc.toJson());

    expect(again.toJson(), doc.toJson());
  });

  test('prescriptions read naturally in both languages', () {
    final day = PlanDocumentModel.fromJson(json).sequence.first;

    expect(const PlanStrings(false).prescription(day.exercises[0]), '3 × 45s');
    expect(const PlanStrings(false).prescription(day.exercises[1]), '4 × 8-10 · 60 kg · RPE 8');
    expect(const PlanStrings(true).prescription(day.exercises[1]), '4 × 8-10 · 60 كجم · RPE 8');
  });

  test('unknown goals fall back to general fitness', () {
    expect(PlanGoal.parse('FatLoss'), PlanGoal.fatLoss);
    expect(PlanGoal.parse('nonsense'), PlanGoal.generalFitness);
  });
}
