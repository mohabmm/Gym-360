import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'ready_programs_section.dart';
import 'trainer_plan_strings.dart';

/// What the coach picked to start a plan from: nothing (blank), a ready program or a template.
class StartChoice {
  final PlanTemplateSummaryModel? template;
  final PlanProgramSummaryModel? program;

  const StartChoice.blank()
      : template = null,
        program = null;

  const StartChoice.template(PlanTemplateSummaryModel this.template) : program = null;

  const StartChoice.program(PlanProgramSummaryModel this.program) : template = null;
}

/// "Start blank" first, then the ready programs, then the gym's and the coach's templates.
/// pt.starter_plan [limit]: a starter plan is built blank or from a ready program's first days
/// and the templates show as locked; an unlocked one-week or 30-day program uses any template or
/// program, cut to fit.
Future<StartChoice?> showStartPlanSheet(BuildContext context, {PlanLimit limit = PlanLimit.none}) =>
    showModalBottomSheet<StartChoice>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => FractionallySizedBox(heightFactor: 0.85, child: _StartPlanSheet(limit: limit)),
    );

class _StartPlanSheet extends ConsumerWidget {
  final PlanLimit limit;

  const _StartPlanSheet({required this.limit});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final s = TrainerPlanStrings(isArabic);
    final templates = ref.watch(planTemplatesProvider);
    final starter = limit == PlanLimit.starter;
    final limited = limit != PlanLimit.none;
    return Directionality(
      textDirection: isArabic ? TextDirection.rtl : TextDirection.ltr,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
        children: [
          Text(limited ? s.limitTitle(limit) : s.newPlan, style: Theme.of(context).textTheme.titleLarge),
          if (limited) Text(s.limitHint(limit)),
          const SizedBox(height: 12),
          Card(
            child: ListTile(
              key: const ValueKey('start-blank'),
              leading: const Icon(Icons.edit_note),
              title: Text(limited ? s.limitBlank(limit) : s.startBlank),
              subtitle: Text(limited ? s.limitRules(limit) : s.startBlankHint),
              onTap: () => Navigator.pop(context, const StartChoice.blank()),
            ),
          ),
          const SizedBox(height: 16),
          if (starter) Text(s.starterFromProgramHint, style: Theme.of(context).textTheme.bodySmall),
          ReadyProgramsSection(strings: s, onTap: (p) => Navigator.pop(context, StartChoice.program(p))),
          const SizedBox(height: 16),
          Text(s.fromTemplate, style: Theme.of(context).textTheme.titleMedium),
          if (starter) _ProgramsLocked(strings: s) else templates.when(
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

/// pt.starter_plan: templates stay closed until the desk unlocks a one-week program.
class _ProgramsLocked extends StatelessWidget {
  final TrainerPlanStrings strings;

  const _ProgramsLocked({required this.strings});

  @override
  Widget build(BuildContext context) => Card(
        key: const ValueKey('programs-locked'),
        child: ListTile(
          leading: const Icon(Icons.lock_outline),
          title: Text(strings.programsLocked),
          subtitle: Text(strings.programsLockedHint),
        ),
      );
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
