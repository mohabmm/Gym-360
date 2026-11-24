import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../clients/presentation/providers/client_providers.dart';
import 'diet_builder_screen.dart';
import 'trainer_diet_strings.dart';

/// Opens the diet builder for [clientId], prefilled with what the app knows about them:
/// sex and age from the profile, weight and goal from their latest measurement. True when given.
Future<bool> startDietForClient(BuildContext context, WidgetRef ref, String clientId) async {
  final prefill = await _prefill(ref, clientId);
  if (!context.mounted) return false;
  final given = await Navigator.of(context).push<bool>(
    MaterialPageRoute(builder: (_) => DietBuilderScreen(clientId: clientId, prefill: prefill)),
  );
  return given ?? false;
}

/// Onboarding: right after a training plan is given, offer the diet too (nutrition on only).
Future<void> offerDietAfterPlan(BuildContext context, WidgetRef ref, String clientId) async {
  if (!ref.read(isFeatureEnabledProvider(FeatureFlagKeys.nutrition))) return;
  final s = TrainerDietStrings(Localizations.localeOf(context).languageCode == 'ar');
  final go = await showDialog<bool>(
    context: context,
    builder: (dialog) => AlertDialog(
      key: const Key('diet-onboarding-offer'),
      icon: const Icon(Icons.restaurant_menu),
      title: Text(s.onboardingTitle),
      content: Text(s.onboardingBody),
      actions: [
        TextButton(onPressed: () => Navigator.pop(dialog, false), child: Text(s.later)),
        FilledButton(onPressed: () => Navigator.pop(dialog, true), child: Text(s.giveDiet)),
      ],
    ),
  );
  if (go == true && context.mounted) await startDietForClient(context, ref, clientId);
}

/// Missing data stays empty for the coach to type; a failed load never blocks the flow.
Future<DietPrefill> _prefill(WidgetRef ref, String clientId) async {
  UserModel? user;
  ProgressJourney? journey;
  try {
    user = (await ref.read(clientDetailProvider(clientId).future)).user;
  } catch (_) {}
  try {
    journey = await ref.read(clientJourneyProvider(clientId).future);
  } catch (_) {}
  return DietPrefill(
    sex: DietSex.fromGender(user?.gender),
    age: _age(user?.dateOfBirth),
    weightKg: _weightKg(journey),
    goal: DietGoal.fromProgressGoal(journey?.goalType),
  );
}

int? _age(DateTime? birth) {
  if (birth == null) return null;
  final now = DateTime.now();
  final hadBirthday = now.month > birth.month || (now.month == birth.month && now.day >= birth.day);
  return now.year - birth.year - (hadBirthday ? 0 : 1);
}

/// The latest reading that has a weight, in kg.
double? _weightKg(ProgressJourney? journey) {
  final entries = journey?.entries ?? const <BodyMetricEntryModel>[];
  for (final e in entries.reversed) {
    final w = e.weight;
    if (w != null) return e.weightUnit?.toLowerCase() == 'lb' ? w * 0.4536 : w;
  }
  return null;
}
