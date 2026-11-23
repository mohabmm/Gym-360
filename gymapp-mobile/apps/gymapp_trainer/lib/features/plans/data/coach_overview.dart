import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';

/// One client in the coach's plan overview (`GET training/coach/plan-overview`).
class CoachClientPlan {
  final String traineeId;
  final String? planId;
  final String? planName;
  final String? planNameAr;
  final int thisWeek;
  final int weekTarget;
  final int doneCount;
  final int total;
  final DateTime? lastWorkoutOn;
  final int? daysSince;
  final bool isInactive;
  final String? nextDayTitle;
  final String? nextDayTitleAr;

  const CoachClientPlan({
    required this.traineeId,
    this.planId,
    this.planName,
    this.planNameAr,
    this.thisWeek = 0,
    this.weekTarget = 0,
    this.doneCount = 0,
    this.total = 0,
    this.lastWorkoutOn,
    this.daysSince,
    this.isInactive = false,
    this.nextDayTitle,
    this.nextDayTitleAr,
  });

  bool get hasPlan => planId != null;

  /// Nothing to say about activity yet: the plan starts later, or it just started and is not overdue.
  bool get notStarted => daysSince == null || (lastWorkoutOn == null && !isInactive);

  String? planDisplayName(bool isArabic) => isArabic && (planNameAr?.isNotEmpty ?? false) ? planNameAr : planName;

  String? nextDay(bool isArabic) => isArabic && (nextDayTitleAr?.isNotEmpty ?? false) ? nextDayTitleAr : nextDayTitle;

  factory CoachClientPlan.fromJson(Map<String, dynamic> json) => CoachClientPlan(
        traineeId: json['traineeId'] as String,
        planId: json['planId'] as String?,
        planName: json['planName'] as String?,
        planNameAr: json['planNameAr'] as String?,
        thisWeek: (json['thisWeek'] as num?)?.toInt() ?? 0,
        weekTarget: (json['weekTarget'] as num?)?.toInt() ?? 0,
        doneCount: (json['doneCount'] as num?)?.toInt() ?? 0,
        total: (json['total'] as num?)?.toInt() ?? 0,
        lastWorkoutOn: DateTime.tryParse(json['lastWorkoutOn'] as String? ?? ''),
        daysSince: (json['daysSince'] as num?)?.toInt(),
        isInactive: json['isInactive'] as bool? ?? false,
        nextDayTitle: json['nextDayTitle'] as String?,
        nextDayTitleAr: json['nextDayTitleAr'] as String?,
      );
}

class CoachPlanOverview {
  final int inactiveDays;
  final List<CoachClientPlan> clients;

  const CoachPlanOverview({this.inactiveDays = 5, this.clients = const []});

  List<CoachClientPlan> get quiet => clients.where((c) => c.isInactive).toList();

  factory CoachPlanOverview.fromJson(Map<String, dynamic> json) => CoachPlanOverview(
        inactiveDays: (json['inactiveDays'] as num?)?.toInt() ?? 5,
        clients: [
          for (final c in (json['clients'] as List? ?? const [])) CoachClientPlan.fromJson(Map<String, dynamic>.from(c as Map)),
        ],
      );
}

final coachPlanOverviewProvider = FutureProvider.autoDispose<CoachPlanOverview>((ref) async {
  return ref.watch(apiClientProvider).get<CoachPlanOverview>(
        ApiEndpoints.coachPlanOverview,
        parser: (data) => CoachPlanOverview.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// Push reminder to a client (the server allows one a day per client: 409 after).
final planNudgeProvider = Provider<Future<void> Function(String traineeId, {String? message})>((ref) {
  final api = ref.watch(apiClientProvider);
  return (traineeId, {message}) => api.post<void>(
        ApiEndpoints.planNudge(traineeId),
        data: {'message': message},
        parser: (_) {},
      );
});

/// A wa.me link for an Egyptian mobile number ("010…", "+20 10…", "0020…"), or null when the
/// number is unusable. WhatsApp is how most Egyptian coaches already talk to their clients.
Uri? whatsAppLink(String? phone, String text) {
  final digits = (phone ?? '').replaceAll(RegExp(r'[^0-9]'), '');
  final international = switch (digits) {
    final d when d.startsWith('0020') => d.substring(2),
    final d when d.startsWith('20') => d,
    final d when d.startsWith('01') => '2$d',
    final d when d.startsWith('1') && d.length == 10 => '20$d',
    _ => null,
  };
  if (international == null || international.length < 11) return null;
  // %20, not '+': WhatsApp shows a '+' in the message literally.
  return Uri.parse('https://wa.me/$international?text=${Uri.encodeComponent(text)}');
}
