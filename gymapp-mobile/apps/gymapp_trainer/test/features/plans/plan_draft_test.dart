import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/plans/data/plan_draft.dart';

PlanDayModel _day(String key, String title) => PlanDayModel(
      key: key,
      title: title,
      exercises: [PlanExerciseModel(key: '$key-e1', name: 'Squat', sets: 3, reps: '10')],
    );

PlanDocumentModel _uniform(int weeks) => PlanDocumentModel(weeks: [
      for (var w = 1; w <= weeks; w++) PlanWeekModel(week: w, days: [_day('w$w-a', 'Day A'), _day('w$w-b', 'Day B')])
    ]);

void main() {
  test('a blank plan is four weeks, same every week, with one empty day', () {
    final draft = PlanDraft.blank();

    expect(draft.durationWeeks, 4);
    expect(draft.sameEveryWeek, isTrue);
    expect(draft.firstWeek, hasLength(1));
    expect(draft.isValid, isFalse, reason: 'it needs a name and exercises');
  });

  test('a template whose weeks are identical opens as "same every week" and keeps week 1 keys', () {
    final draft = PlanDraft.fromDocument(_uniform(4), name: 'Full body');

    expect(draft.sameEveryWeek, isTrue);
    expect(draft.weeks, hasLength(1));
    expect(draft.durationWeeks, 4);
    expect(draft.workoutCount, 8);
    expect(draft.firstWeek.first.key, 'w1-a');
  });

  test('the request repeats week 1 for every week when same every week', () {
    final json = PlanDraft.fromDocument(_uniform(4), name: 'Full body').toContentJson();

    expect(json['repeatWeekOneForWeeks'], 4);
    expect((json['document'] as Map)['weeks'], hasLength(1));
    expect(json['goal'], 'GeneralFitness');
  });

  test('splitting weeks copies week 1 without keys; copy week 1 to all collapses them again', () {
    final draft = PlanDraft.fromDocument(_uniform(4), name: 'Full body')..splitWeeks();

    expect(draft.weeks, hasLength(4));
    expect(draft.weeks[1].first.key, isEmpty, reason: 'the server gives copies fresh keys');
    expect(draft.weeks[0].first.key, 'w1-a');
    expect(draft.toContentJson()['repeatWeekOneForWeeks'], isNull);

    draft.weeks[2].first.title = 'Changed';
    draft.copyWeekOneToAll();
    expect(draft.weeks, hasLength(1));
    expect(draft.sameEveryWeek, isTrue);
  });

  test('changing the duration of a week-by-week plan adds copies of the last week or drops weeks', () {
    final draft = PlanDraft.fromDocument(_uniform(2), name: 'P')..splitWeeks();
    draft.weeks[1].first.title = 'Heavier';

    draft.setDurationWeeks(4);
    expect(draft.weeks.map((w) => w.first.title), ['Day A', 'Heavier', 'Heavier', 'Heavier']);

    draft.setDurationWeeks(1);
    expect(draft.weeks, hasLength(1));
  });

  test('reordering exercises moves one to the new place', () {
    final day = DayDraft(exercises: [ExerciseDraft(name: 'A'), ExerciseDraft(name: 'B'), ExerciseDraft(name: 'C')]);

    day.moveExercise(0, 2);

    expect(day.exercises.map((e) => e.name), ['B', 'C', 'A']);
  });

  test('a timed exercise needs a time, not reps', () {
    expect(ExerciseDraft(name: 'Plank', reps: '', seconds: 45).isValid, isTrue);
    expect(ExerciseDraft(name: 'Plank', reps: '').isValid, isFalse);
    expect(ExerciseDraft(name: 'Plank', seconds: 45).toModel().reps, isNull);
  });

  test('a free-text goal needs a description', () {
    final draft = PlanDraft.fromDocument(_uniform(1), name: 'P', goal: PlanGoal.other);
    expect(draft.isValid, isFalse);
    draft.goalText = 'Football pre-season';
    expect(draft.isValid, isTrue);
  });

  test('days left unnamed are named in order, so a plan from scratch can be saved', () {
    final draft = PlanDraft.blank()
      ..name = 'خطتي'
      ..firstWeek.add(DayDraft(title: 'Legs'));
    for (final d in draft.firstWeek) {
      d.exercises.add(ExerciseDraft(name: 'Squat'));
    }
    expect(draft.isValid, isFalse, reason: 'day 1 has no name yet');

    draft.nameUnnamedDays(isArabic: true);

    expect(draft.isValid, isTrue);
    expect(draft.firstWeek[0].title, 'Day 1');
    expect(draft.firstWeek[0].titleAr, 'اليوم ١');
    expect(draft.firstWeek[1].title, 'Legs', reason: 'a name the coach typed is kept');
  });
}
