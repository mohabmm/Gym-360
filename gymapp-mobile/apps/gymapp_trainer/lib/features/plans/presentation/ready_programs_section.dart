import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_api.dart';
import 'trainer_plan_strings.dart';

/// "Ready programs" (برامج جاهزة): the built-in programs with a category filter. Used by the
/// start-a-plan sheet and the templates screen; [onTap] decides what picking one does.
class ReadyProgramsSection extends ConsumerStatefulWidget {
  final TrainerPlanStrings strings;
  final ValueChanged<PlanProgramSummaryModel> onTap;

  const ReadyProgramsSection({super.key, required this.strings, required this.onTap});

  @override
  ConsumerState<ReadyProgramsSection> createState() => _ReadyProgramsSectionState();
}

class _ReadyProgramsSectionState extends ConsumerState<ReadyProgramsSection> {
  ProgramCategory? _category;

  @override
  Widget build(BuildContext context) {
    final s = widget.strings;
    final programs = ref.watch(planProgramsProvider);
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Text(s.readyPrograms, style: Theme.of(context).textTheme.titleMedium),
      Text(s.readyProgramsHint, style: Theme.of(context).textTheme.bodySmall),
      const SizedBox(height: 8),
      SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(children: [
          _chip(s.allCategories, null),
          for (final c in ProgramCategory.values) _chip(s.category(c), c),
        ]),
      ),
      programs.when(
        loading: () => const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator())),
        error: (_, __) => Padding(padding: const EdgeInsets.all(16), child: Text(s.saveFailed)),
        data: (list) => Column(children: [
          for (final p in list.where((p) => _category == null || p.category == _category))
            ReadyProgramTile(program: p, strings: s, onTap: () => widget.onTap(p)),
        ]),
      ),
    ]);
  }

  Widget _chip(String label, ProgramCategory? value) => Padding(
        padding: const EdgeInsetsDirectional.only(end: 6),
        child: ChoiceChip(label: Text(label), selected: _category == value, onSelected: (_) => setState(() => _category = value)),
      );
}

/// One ready program: name, what it is for, level, days a week and weeks.
class ReadyProgramTile extends StatelessWidget {
  final PlanProgramSummaryModel program;
  final TrainerPlanStrings strings;
  final VoidCallback onTap;

  const ReadyProgramTile({super.key, required this.program, required this.strings, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final s = strings;
    final p = program;
    return Card(
      child: ListTile(
        leading: const Icon(Icons.auto_awesome_outlined),
        title: Text(p.displayName(s.isArabic)),
        subtitle: Text([
          p.displayDescription(s.isArabic),
          '${s.level(p.isBeginner)} · ${s.goal(p.goal)} · ${s.daysPerWeek(p.daysPerWeek)} · ${s.weeks(p.durationWeeks)}',
        ].where((line) => line.isNotEmpty).join('\n')),
        isThreeLine: true,
        onTap: onTap,
      ),
    );
  }
}
