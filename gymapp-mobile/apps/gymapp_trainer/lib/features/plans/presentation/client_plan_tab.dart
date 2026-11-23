import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_api.dart';
import 'plan_flow.dart';
import 'trainer_plan_strings.dart';

/// pt.training_plan — the client's plan on their page: what they follow now, a quick way to give
/// or edit a plan, and the plans they had before.
class ClientPlanTab extends ConsumerWidget {
  final String clientId;
  final bool isArabic;

  const ClientPlanTab({super.key, required this.clientId, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = TrainerPlanStrings(isArabic);
    final plans = ref.watch(traineePlansProvider(clientId));
    return RefreshIndicator(
      onRefresh: () => ref.refresh(traineePlansProvider(clientId).future),
      child: plans.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => ListView(children: [Padding(padding: const EdgeInsets.all(24), child: Text(s.saveFailed))]),
        data: (data) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (data.active == null) _NoPlan(strings: s, onGive: () => startPlanForClient(context, ref, clientId))
            else _ActivePlan(plan: data.active!, strings: s, clientId: clientId),
            if (data.history.isNotEmpty) ...[
              const SizedBox(height: 16),
              Text(s.history, style: Theme.of(context).textTheme.titleSmall),
              for (final p in data.history)
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.history),
                  title: Text(p.displayName(isArabic)),
                  subtitle: Text('${s.goal(p.goal, p.goalText)} · ${s.weeks(p.durationWeeks)} · '
                      '${MaterialLocalizations.of(context).formatMediumDate(p.startDate)}'),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

class _NoPlan extends StatelessWidget {
  final TrainerPlanStrings strings;
  final VoidCallback onGive;

  const _NoPlan({required this.strings, required this.onGive});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text(strings.noPlan, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(strings.noPlanHint),
            const SizedBox(height: 12),
            FilledButton.icon(onPressed: onGive, icon: const Icon(Icons.add_task), label: Text(strings.givePlan)),
          ]),
        ),
      );
}

class _ActivePlan extends ConsumerWidget {
  final TrainingPlanModel plan;
  final TrainerPlanStrings strings;
  final String clientId;

  const _ActivePlan({required this.plan, required this.strings, required this.clientId});

  Future<void> _saveAsTemplate(BuildContext context, WidgetRef ref) async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(planApiProvider).savePlanAsTemplate(plan.summary.id, plan.summary.displayName(strings.isArabic));
      messenger.showSnackBar(SnackBar(content: Text(strings.saved)));
    } on ApiException {
      messenger.showSnackBar(SnackBar(content: Text(strings.saveFailed)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = strings;
    final p = plan.summary;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text(p.displayName(s.isArabic), style: Theme.of(context).textTheme.titleMedium),
          Text('${s.goal(p.goal, p.goalText)} · ${s.daysPerWeek(p.daysPerWeek)} · ${s.weeks(p.durationWeeks)}'),
          Text('${s.startDate}: ${MaterialLocalizations.of(context).formatMediumDate(p.startDate)}'),
          const Divider(height: 24),
          for (final day in plan.document.weeks.isEmpty ? const <PlanDayModel>[] : plan.document.weeks.first.days)
            ListTile(
              dense: true,
              contentPadding: EdgeInsets.zero,
              title: Text(day.displayTitle(s.isArabic)),
              subtitle: Text(day.exercises.map((e) => e.displayName(s.isArabic)).join('، ')),
            ),
          const SizedBox(height: 8),
          Wrap(spacing: 8, runSpacing: 8, children: [
            FilledButton.icon(onPressed: () => editClientPlan(context, plan), icon: const Icon(Icons.edit), label: Text(s.editPlan)),
            OutlinedButton.icon(
              onPressed: () => startPlanForClient(context, ref, clientId),
              icon: const Icon(Icons.swap_horiz),
              label: Text(s.replacePlan),
            ),
            TextButton.icon(
              onPressed: () => _saveAsTemplate(context, ref),
              icon: const Icon(Icons.bookmark_add_outlined),
              label: Text(s.saveAsTemplate),
            ),
          ]),
        ]),
      ),
    );
  }
}
