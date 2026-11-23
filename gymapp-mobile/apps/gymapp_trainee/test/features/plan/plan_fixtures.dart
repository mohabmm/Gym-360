import 'package:gymapp_core/gymapp_core.dart';

/// A 2-week plan, 2 days a week, as `GET training/me/plan` sends it.
Map<String, dynamic> myPlanJson({
  String? nextDayKey = 'w1a',
  int lastDoneIndex = -1,
  List<String> doneKeys = const [],
  bool doneToday = false,
  int thisWeek = 0,
  Map<String, double> lastWeights = const {'e-squat-1': 40},
}) =>
    {
      'plan': {
        'summary': {
          'id': 'plan-1',
          'traineeId': 'user-1',
          'trainerId': 'coach-1',
          'name': 'Beginner full body',
          'nameAr': 'فول بادي للمبتدئين',
          'goal': 'GeneralFitness',
          'durationWeeks': 2,
          'daysPerWeek': 2,
          'workoutCount': 4,
          'startDate': '2026-11-21',
          'status': 'Active',
          'version': 3,
          'updatedAt': '2026-11-21T10:00:00Z',
        },
        'document': {
          'schemaVersion': 1,
          'weeks': [
            for (final w in [1, 2])
              {
                'week': w,
                'days': [
                  {
                    'key': 'w${w}a',
                    'title': 'Day A',
                    'titleAr': 'اليوم أ',
                    'exercises': [
                      {'key': 'e-squat-$w', 'name': 'Goblet Squat', 'nameAr': 'سكوات', 'sets': 2, 'reps': '10-12', 'restSeconds': 60},
                      {'key': 'e-plank-$w', 'name': 'Plank', 'sets': 1, 'seconds': 30, 'restSeconds': 30},
                    ],
                  },
                  {
                    'key': 'w${w}b',
                    'title': 'Day B',
                    'titleAr': 'اليوم ب',
                    'exercises': [
                      {'key': 'e-push-$w', 'name': 'Push-up', 'sets': 3, 'reps': '10'},
                    ],
                  },
                ],
              },
          ],
        },
      },
      'cursor': {
        'nextDayKey': nextDayKey,
        'nextWeek': 1,
        'lastDoneIndex': lastDoneIndex,
        'total': 4,
        'finished': nextDayKey == null,
        'doneToday': doneToday,
        'doneKeys': doneKeys,
      },
      'stats': {'thisWeek': thisWeek, 'weekTarget': 2, 'thisMonth': thisWeek, 'weekStart': '2026-11-21'},
      'lastWeights': lastWeights,
    };

MyPlanModel myPlan({String? nextDayKey = 'w1a', int lastDoneIndex = -1}) =>
    MyPlanModel.fromJson(myPlanJson(nextDayKey: nextDayKey, lastDoneIndex: lastDoneIndex));
