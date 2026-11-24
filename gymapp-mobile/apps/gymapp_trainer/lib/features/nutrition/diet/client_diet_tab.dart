import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'diet_api.dart';
import 'diet_flow.dart';
import 'trainer_diet_strings.dart';

/// The client's diet on their page: their target, how many meals they ticked each of the
/// last 7 days (to talk about at the next session), and giving or replacing the diet.
class ClientDietTab extends ConsumerWidget {
  final String clientId;
  final bool isArabic;

  const ClientDietTab({super.key, required this.clientId, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = TrainerDietStrings(isArabic);
    final diet = ref.watch(clientDietProvider(clientId));
    return RefreshIndicator(
      onRefresh: () => ref.refresh(clientDietProvider(clientId).future),
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: diet.when(
          loading: () => const [Center(child: CircularProgressIndicator())],
          error: (_, __) => [Text(s.failed)],
          data: (d) => [
            if (d.plan == null) _Empty(s: s) else _Current(diet: d, s: s),
            const SizedBox(height: 12),
            FilledButton.icon(
              key: const Key('client-diet-give'),
              onPressed: () => startDietForClient(context, ref, clientId),
              icon: const Icon(Icons.restaurant_menu),
              label: Text(d.plan == null ? s.giveDiet : s.newDiet),
            ),
          ],
        ),
      ),
    );
  }
}

class _Empty extends StatelessWidget {
  final TrainerDietStrings s;

  const _Empty({required this.s});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(s.noDiet, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(s.noDietHint),
          ]),
        ),
      );
}

class _Current extends StatelessWidget {
  final ClientDietModel diet;
  final TrainerDietStrings s;

  const _Current({required this.diet, required this.s});

  @override
  Widget build(BuildContext context) {
    final plan = diet.plan!;
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(plan.displayName(s.isArabic), style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          Text('${s.dailyTarget}: ${s.kcal(plan.dailyCalories)}'),
          Text('${s.protein} ${s.grams(plan.proteinGrams)} · ${s.carbs} ${s.grams(plan.carbsGrams)} · ${s.fat} ${s.grams(plan.fatGrams)}',
              style: theme.textTheme.bodySmall),
          const Divider(height: 20),
          Text(s.lastWeek(diet), key: const Key('client-diet-week')),
          const SizedBox(height: 8),
          _WeekBars(days: diet.lastWeek, s: s),
          const SizedBox(height: 6),
          Text(s.talkAtSession, style: theme.textTheme.bodySmall),
        ]),
      ),
    );
  }
}

class _WeekBars extends StatelessWidget {
  final List<DietDayAdherenceModel> days;
  final TrainerDietStrings s;

  const _WeekBars({required this.days, required this.s});

  @override
  Widget build(BuildContext context) => Row(children: [
        for (final d in days)
          Expanded(
            child: Column(children: [
              SizedBox(
                height: 48,
                width: 14,
                child: RotatedBox(
                  quarterTurns: 3,
                  child: LinearProgressIndicator(
                    value: d.mealsPlanned == 0 ? 0 : d.mealsDone / d.mealsPlanned,
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
              ),
              const SizedBox(height: 4),
              Text(s.weekday(d.date.weekday % 7).substring(0, s.isArabic ? 2 : 3), style: Theme.of(context).textTheme.labelSmall),
            ]),
          ),
      ]);
}
