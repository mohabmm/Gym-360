import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';
import 'plan_api.dart';

/// pt.training_plan: the client's plan days the coach can count a PT session as.
/// [suggestedKey] is the trainee's next day (pre-selected); the coach can pick another or none.
class SessionPlanDays {
  final String planId;
  final List<PlanSlot> slots;
  final String? suggestedKey;

  const SessionPlanDays({required this.planId, required this.slots, this.suggestedKey});

  /// The days shown: the week of the suggested day (week 1 when there is none).
  List<PlanSlot> get shown {
    final week = slots.where((s) => s.day.key == suggestedKey).firstOrNull?.week ?? slots.firstOrNull?.week;
    return slots.where((s) => s.week == week).toList();
  }

  /// `planDay` for the quick-log payload.
  Map<String, dynamic> payload(String dayKey) => {'planId': planId, 'dayKey': dayKey};
}

/// null when plans are off or the client has no active plan. Never fails the session sheet:
/// if the next day cannot be loaded, the days are offered without a suggestion.
final sessionPlanDaysProvider = FutureProvider.autoDispose.family<SessionPlanDays?, String>((ref, clientId) async {
  if (!ref.watch(isFeatureEnabledProvider(FeatureFlagKeys.ptTrainingPlan))) return null;
  final plans = await ref.watch(traineePlansProvider(clientId).future);
  final active = plans.active;
  if (active == null) return null;
  return SessionPlanDays(
    planId: active.summary.id,
    slots: PlanSlot.of(active.document),
    suggestedKey: await _nextDayKey(ref, clientId),
  );
});

Future<String?> _nextDayKey(Ref ref, String clientId) async {
  try {
    final progress = await ref.read(apiClientProvider).get<PlanProgressModel>(
          ApiEndpoints.traineePlanProgress(clientId),
          parser: (data) => PlanProgressModel.fromJson(Map<String, dynamic>.from(data as Map)),
        );
    return progress.cursor?.nextDayKey;
  } on ApiException {
    return null;
  }
}
