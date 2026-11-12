import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// A trainee who finished a free trial and has not bought a package (pt.trial_end_alerts).
class NotConvertedTrial {
  final String packageId;
  final String memberId;
  final String memberName;
  final String trainerName;
  final DateTime finishedAt;
  final int daysSinceFinished;
  final bool windowClosed;

  const NotConvertedTrial({
    required this.packageId,
    required this.memberId,
    required this.memberName,
    required this.trainerName,
    required this.finishedAt,
    required this.daysSinceFinished,
    required this.windowClosed,
  });

  factory NotConvertedTrial.fromJson(Map<String, dynamic> j) =>
      NotConvertedTrial(
        packageId: j['packageId'] as String? ?? '',
        memberId: j['memberId'] as String? ?? '',
        memberName: j['memberName'] as String? ?? '',
        trainerName: j['trainerName'] as String? ?? '',
        finishedAt: DateTime.tryParse(j['finishedAt'] as String? ?? '') ??
            DateTime.now(),
        daysSinceFinished: (j['daysSinceFinished'] as num?)?.toInt() ?? 0,
        windowClosed: j['conversionWindowClosed'] == true,
      );
}

/// One coach row of the PT owner dashboard (pt.owner_dashboard).
class PtCoachRow {
  final String trainerId;
  final String trainerName;
  final int trialsFinished;
  final int trialsConverted;
  final double conversionRate;
  final int renewals;
  final int newPaidPackages;
  final int sessionsLogged;

  const PtCoachRow({
    required this.trainerId,
    required this.trainerName,
    required this.trialsFinished,
    required this.trialsConverted,
    required this.conversionRate,
    required this.renewals,
    required this.newPaidPackages,
    required this.sessionsLogged,
  });

  factory PtCoachRow.fromJson(Map<String, dynamic> j) => PtCoachRow(
        trainerId: j['trainerId'] as String? ?? '',
        trainerName: j['trainerName'] as String? ?? '',
        trialsFinished: (j['trialsFinished'] as num?)?.toInt() ?? 0,
        trialsConverted: (j['trialsConverted'] as num?)?.toInt() ?? 0,
        conversionRate: (j['conversionRate'] as num?)?.toDouble() ?? 0,
        renewals: (j['renewals'] as num?)?.toInt() ?? 0,
        newPaidPackages: (j['newPaidPackages'] as num?)?.toInt() ?? 0,
        sessionsLogged: (j['sessionsLogged'] as num?)?.toInt() ?? 0,
      );
}

class PtDashboard {
  final PtCoachRow totals;
  final int activeTrials;
  final int notConverted;
  final List<PtCoachRow> coaches;

  const PtDashboard(
      {required this.totals,
      required this.activeTrials,
      required this.notConverted,
      required this.coaches});

  factory PtDashboard.fromJson(Map<String, dynamic> j) {
    final t = Map<String, dynamic>.from(j['totals'] as Map? ?? const {});
    return PtDashboard(
      totals: PtCoachRow.fromJson({...t, 'trainerId': '', 'trainerName': ''}),
      activeTrials: (t['activeTrials'] as num?)?.toInt() ?? 0,
      notConverted: (t['notConvertedTrials'] as num?)?.toInt() ?? 0,
      coaches: (j['coaches'] as List? ?? const [])
          .map((e) => PtCoachRow.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(),
    );
  }
}

final notConvertedTrialsProvider =
    FutureProvider.autoDispose<List<NotConvertedTrial>>((ref) async {
  return ref.watch(apiClientProvider).get<List<NotConvertedTrial>>(
        ApiEndpoints.trialsNotConverted,
        parser: (data) => (data as List)
            .map((e) =>
                NotConvertedTrial.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList(),
      );
});

/// Sessions the trainee disputed ("not me") waiting for the desk.
final disputedSessionsProvider =
    FutureProvider.autoDispose<List<SessionModel>>((ref) async {
  return ref.watch(apiClientProvider).get<List<SessionModel>>(
        ApiEndpoints.sessionsForReview,
        parser: (data) => (data as List)
            .map((e) =>
                SessionModel.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList(),
      );
});

/// PT owner dashboard for the last [days] days.
final ptDashboardProvider =
    FutureProvider.autoDispose.family<PtDashboard, int>((ref, days) async {
  final to = DateTime.now().toUtc();
  final from = to.subtract(Duration(days: days));
  return ref.watch(apiClientProvider).get<PtDashboard>(
        ApiEndpoints.ptDashboard,
        queryParameters: {
          'from': from.toIso8601String(),
          'to': to.toIso8601String()
        },
        parser: (data) =>
            PtDashboard.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});
