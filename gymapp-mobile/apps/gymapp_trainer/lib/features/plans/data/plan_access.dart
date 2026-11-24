import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';
import 'plan_draft.dart';

/// pt.starter_plan: the client's active 3-day plan or one-week program, if any.
class LimitedPlanStatus {
  final String planId;
  final PlanLimit limit;
  final DateTime endsOn;
  final bool hasEnded;

  const LimitedPlanStatus({required this.planId, required this.limit, required this.endsOn, required this.hasEnded});

  static LimitedPlanStatus? fromJson(Object? json) {
    if (json is! Map) return null;
    final map = Map<String, dynamic>.from(json);
    return LimitedPlanStatus(
      planId: map['planId'] as String? ?? '',
      limit: PlanLimit.parse(map['limit'] as String?),
      endsOn: DateTime.tryParse(map['endsOn'] as String? ?? '') ?? DateTime.now(),
      hasEnded: map['hasEnded'] as bool? ?? false,
    );
  }
}

/// pt.starter_plan: what the coach may give this client now — a 3-day starter plan, one week
/// from a template or ready program (each staff unlock allows one), or anything (flag off).
class CoachPlanAccess {
  final PlanLimit limit;
  final LimitedPlanStatus? limitedPlan;

  const CoachPlanAccess({this.limit = PlanLimit.none, this.limitedPlan});

  bool get isStarter => limit == PlanLimit.starter;

  factory CoachPlanAccess.fromJson(Map<String, dynamic> json) => CoachPlanAccess(
        limit: PlanLimit.parse(json['limit'] as String?),
        limitedPlan: LimitedPlanStatus.fromJson(json['limitedPlan']),
      );
}

/// The server enforces the limit on every plan it saves, so an error here falls back to "no limit".
final coachPlanAccessProvider = FutureProvider.autoDispose.family<CoachPlanAccess, String>((ref, clientId) async {
  try {
    return await ref.watch(apiClientProvider).get<CoachPlanAccess>(
          ApiEndpoints.traineePlanAccess(clientId),
          parser: (data) => CoachPlanAccess.fromJson(Map<String, dynamic>.from(data as Map)),
        );
  } on ApiException {
    return const CoachPlanAccess();
  }
});
