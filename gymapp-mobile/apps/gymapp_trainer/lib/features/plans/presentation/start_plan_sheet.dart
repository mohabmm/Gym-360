import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_api.dart';
import 'trainer_plan_strings.dart';

/// What the coach picked to start a plan from: nothing (blank) or a template.
class StartChoice {
  final PlanTemplateSummaryModel? template;

  const StartChoice.blank() : template = null;

  const StartChoice.template(PlanTemplateSummaryModel this.template);
}

/// "Start blank" next to the gym's and the coach's templates — both are one tap away.
Future<StartChoice?> showStartPlanSheet(BuildContext context) => showModalBottomSheet<StartChoice>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => const FractionallySizedBox(heightFactor: 0.85, child: _StartPlanSheet()),
    );

class _StartPlanSheet extends ConsumerWidget {
  const _StartPlanSheet();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final s = TrainerPlanStrings(isArabic);
    final templates = ref.watch(planTemplatesProvider);
    return Directionality(
      textDirection: isArabic ? TextDirection.rtl : TextDirection.ltr,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
        children: [
          Text(s.newPlan, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 12),
          Card(
            child: ListTile(
              leading: const Icon(Icons.edit_note),
              title: Text(s.startBlank),
              subtitle: Text(s.startBlankHint),
              onTap: () => Navigator.pop(context, const StartChoice.blank()),
            ),
          ),
          const SizedBox(height: 16),
          Text(s.fromTemplate, style: Theme.of(context).textTheme.titleMedium),
          templates.when(
            loading: () => const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator())),
            error: (_, __) => Padding(padding: const EdgeInsets.all(16), child: Text(s.saveFailed)),
            data: (list) => Column(children: [
              ..._section(context, s, s.gymTemplates, list.where((t) => t.isGymTemplate)),
              ..._section(context, s, s.myTemplates, list.where((t) => !t.isGymTemplate)),
            ]),
          ),
        ],
      ),
    );
  }

  List<Widget> _section(BuildContext context, TrainerPlanStrings s, String title, Iterable<PlanTemplateSummaryModel> items) {
    if (items.isEmpty) return const [];
    return [
      Padding(
        padding: const EdgeInsetsDirectional.only(top: 12, bottom: 4),
        child: Align(alignment: AlignmentDirectional.centerStart, child: Text(title, style: Theme.of(context).textTheme.labelLarge)),
      ),
      for (final t in items) PlanTemplateTile(template: t, strings: s, onTap: () => Navigator.pop(context, StartChoice.template(t))),
    ];
  }
}

/// One template: name, goal, days a week and weeks.
class PlanTemplateTile extends StatelessWidget {
  final PlanTemplateSummaryModel template;
  final TrainerPlanStrings strings;
  final VoidCallback onTap;

  const PlanTemplateTile({super.key, required this.template, required this.strings, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final s = strings;
    return Card(
      child: ListTile(
        leading: Icon(template.isGymTemplate ? Icons.storefront_outlined : Icons.person_outline),
        title: Text(template.displayName(s.isArabic)),
        subtitle: Text('${s.goal(template.goal, template.goalText)} · ${s.daysPerWeek(template.daysPerWeek)} · ${s.weeks(template.durationWeeks)}'),
        trailing: template.isSample ? Chip(label: Text(s.sample), visualDensity: VisualDensity.compact) : null,
        onTap: onTap,
      ),
    );
  }
}
