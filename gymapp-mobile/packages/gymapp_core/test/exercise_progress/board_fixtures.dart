import 'package:gymapp_core/gymapp_core.dart';

/// API-shaped JSON for a trial member: Push Day (bench, 2 sessions) + Pull Day (row, 1 session).
Map<String, dynamic> trialBoardJson({bool paid = false, bool withSessions = true}) => {
      'summary': {
        'sessionsLogged': 3,
        'trialSessionsUsed': 3,
        'trialSessionsTotal': 20,
        'hasPaidPackage': paid,
        'isTrialView': !paid,
        'firstSessionDate': '2026-11-01',
        'lastSessionDate': '2026-11-08',
      },
      'days': [
        {'label': 'Push Day', 'sessionsCount': 2, 'lastTrained': '2026-11-08', 'exerciseKeys': ['bench press']},
        {'label': 'Pull Day', 'sessionsCount': 1, 'lastTrained': '2026-11-03', 'exerciseKeys': ['barbell row']},
      ],
      'exercises': [
        {
          'key': 'bench press',
          'name': 'Bench press',
          'nameAr': 'ضغط صدر',
          'entries': [
            _entry(1, '2026-11-01', 'Push Day', '10,8,8', 60, 1560, 80, notes: 'Elbows in', target: {'sets': 3, 'reps': '10', 'weight': 62.5}),
            _entry(3, '2026-11-08', 'Push Day', '10,10,10', 62.5, 1875, 83.5, best: true, hit: true,
                target: {'sets': 3, 'reps': '10', 'weight': 65}),
          ],
          'trend': {
            'firstWeight': 60,
            'latestWeight': 62.5,
            'bestWeight': 62.5,
            'weightChangePercent': 4.2,
            'latestVolume': 1875,
            'volumeChangePercent': 20.2,
            'latestEstimatedOneRepMax': 83.5,
            'latestIsPersonalBest': true,
            'hitPreviousTarget': true,
          },
          'nextTarget': {'sets': 3, 'reps': '10', 'weight': 65},
        },
        {
          'key': 'barbell row',
          'name': 'Barbell row',
          'entries': [_entry(2, '2026-11-03', 'Pull Day', '10', 50, 1500, 66.5)],
          'trend': {'firstWeight': 50, 'latestWeight': 50, 'bestWeight': 50, 'latestIsPersonalBest': false},
          'nextTarget': null,
        },
      ],
      if (withSessions)
        'sessions': [
          _session(1, '2026-11-01', 'Push Day', ['bench press']),
          _session(2, '2026-11-03', 'Pull Day', ['barbell row']),
          _session(3, '2026-11-08', 'Push Day', ['bench press'],
              recap: {'feedback': 'Great energy today', 'nextSessionFocus': 'Add 2.5 kg on bench', 'effortRating': 4}),
        ],
    };

Map<String, dynamic> _session(int n, String date, String day, List<String> keys, {Map<String, dynamic>? recap}) => {
      'sessionId': 's$n',
      'sessionNumber': n,
      'date': date,
      'dayLabel': day,
      'isTrialSession': true,
      'exerciseKeys': keys,
      'coachRecap': recap,
    };

Map<String, dynamic> _entry(int n, String date, String day, String reps, num weight, num volume, num oneRm,
        {String? notes, Map<String, dynamic>? target, bool best = false, bool? hit}) =>
    {
      'sessionId': 's$n',
      'date': date,
      'sessionNumber': n,
      'isTrialSession': true,
      'dayLabel': day,
      'sets': 3,
      'reps': reps,
      'totalReps': 26,
      'weight': weight,
      'volume': volume,
      'estimatedOneRepMax': oneRm,
      'notes': notes,
      'target': target,
      'isPersonalBest': best,
      'hitPreviousTarget': hit,
    };

ExerciseProgressModel trialBoard({bool paid = false, bool withSessions = true}) =>
    ExerciseProgressModel.fromJson(trialBoardJson(paid: paid, withSessions: withSessions));
