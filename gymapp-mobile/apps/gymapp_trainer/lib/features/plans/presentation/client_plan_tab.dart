import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/coach_overview.dart';
import '../data/plan_access.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'client_plans_strings.dart';
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
    final access = ref.watch(coachPlanAccessProvider(clientId)).valueOrNull ?? const CoachPlanAccess();
    return RefreshIndicator(
      onRefresh: () {
        ref.invalidate(coachPlanAccessProvider(clientId));
        return ref.refresh(traineePlansProvider(clientId).future);
      },
      child: plans.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => ListView(children: [Padding(padding: const EdgeInsets.all(24), child: Text(s.saveFailed))]),
        data: (data) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (data.active == null)
              _NoPlan(strings: s, limit: access.limit, onGive: () => startPlanForClient(context, ref, clientId))
            else
              _ActivePlan(plan: data.active!, strings: s, clientId: clientId, access: access),
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
  final PlanLimit limit;
  final VoidCallback onGive;

  const _NoPlan({required this.strings, required this.limit, required this.onGive});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text(strings.noPlan, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(limit == PlanLimit.none ? strings.noPlanHint : strings.limitHint(limit)),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: onGive,
              icon: const Icon(Icons.add_task),
              label: Text(limit == PlanLimit.none ? strings.givePlan : strings.limitTitle(limit)),
            ),
          ]),
        ),
      );
}

class _ActivePlan extends ConsumerWidget {
  final TrainingPlanModel plan;
  final TrainerPlanStrings strings;
  final String clientId;
  final CoachPlanAccess access;

  const _ActivePlan({required this.plan, required this.strings, required this.clientId, required this.access});

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
          if (p.limit != 'None') _LimitStatus(plan: p, strings: s),
          _Activity(clientId: clientId, strings: ClientPlansStrings(s.isArabic)),
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
            FilledButton.icon(
              onPressed: () => editClientPlan(context, plan),
              icon: const Icon(Icons.edit),
              label: Text(s.editPlan),
            ),
            OutlinedButton.icon(
              onPressed: () => startPlanForClient(context, ref, clientId),
              icon: const Icon(Icons.swap_horiz),
              label: Text(access.limit.isUnlocked ? s.limitTitle(access.limit) : s.replacePlan),
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

/// pt.starter_plan: "Starter plan / One-week program · last day …", or that it is over.
class _LimitStatus extends StatelessWidget {
  final TrainingPlanSummaryModel plan;
  final TrainerPlanStrings strings;

  const _LimitStatus({required this.plan, required this.strings});

  @override
  Widget build(BuildContext context) {
    final limit = PlanLimit.parse(plan.limit);
    final endsOn = plan.endsOn;
    final text = plan.hasEnded || endsOn == null
        ? strings.limitEnded(limit)
        : strings.limitEnds(limit, MaterialLocalizations.of(context).formatMediumDate(endsOn));
    return Padding(
      key: const ValueKey('limit-status'),
      padding: const EdgeInsets.only(top: 8),
      child: Text(
        text,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: plan.hasEnded ? AppColors.warning : null,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}

/// "This week 1/3 · 4/12 done · last workout 2 days ago" from the coach's overview.
class _Activity extends ConsumerWidget {
  final String clientId;
  final ClientPlansStrings strings;

  const _Activity({required this.clientId, required this.strings});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final row = ref
        .watch(coachPlanOverviewProvider)
        .valueOrNull
        ?.clients
        .where((c) => c.traineeId == clientId && c.hasPlan)
        .firstOrNull;
    if (row == null) return const SizedBox.shrink();
    return Padding(
      key: const ValueKey('client-plan-activity'),
      padding: const EdgeInsets.only(top: 8),
      child: Text(
        '${strings.progress(row)} · ${strings.activity(row)}',
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: row.isInactive ? AppColors.warning : null,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}
