import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'plan_builder_header.dart';
import 'plan_day_card.dart';
import 'trainer_plan_strings.dart';

enum PlanBuilderMode { assign, editPlan, newTemplate, editTemplate }

/// What the builder opens with and what its main button does.
class PlanBuilderArgs {
  final PlanBuilderMode mode;
  final PlanDraft draft;
  final String? clientId;
  final TrainingPlanSummaryModel? plan;
  final String? templateId;

  const PlanBuilderArgs({required this.mode, required this.draft, this.clientId, this.plan, this.templateId});
}

/// The plan builder: name and goal, weeks, then workout days and their exercises. The same screen
/// gives a client a plan, edits their plan, or makes and edits a template.
class PlanBuilderScreen extends ConsumerStatefulWidget {
  final PlanBuilderArgs args;

  const PlanBuilderScreen({super.key, required this.args});

  @override
  ConsumerState<PlanBuilderScreen> createState() => _PlanBuilderScreenState();
}

class _PlanBuilderScreenState extends ConsumerState<PlanBuilderScreen> {
  late final PlanDraft _draft = widget.args.draft;
  DateTime _startDate = DateUtils.dateOnly(DateTime.now());
  int _week = 0;
  bool _saving = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  PlanBuilderMode get _mode => widget.args.mode;

  List<DayDraft> get _days => _draft.weeks[_draft.sameEveryWeek ? 0 : _week.clamp(0, _draft.weeks.length - 1)];

  void _changed() => setState(() {});

  Future<void> _save(TrainerPlanStrings s) async {
    _draft.nameUnnamedDays(isArabic: s.isArabic);
    if (!_draft.isValid) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(s.incomplete)));
      return;
    }
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _saving = true);
    try {
      await _submit();
      messenger.showSnackBar(SnackBar(content: Text(_mode == PlanBuilderMode.assign ? s.assigned : s.saved)));
      router.pop(true);
    } on ApiException catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(_errorText(s, e))));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _submit() {
    final api = ref.read(planApiProvider);
    final a = widget.args;
    return switch (_mode) {
      PlanBuilderMode.assign =>
        api.assign(clientId: a.clientId!, draft: _draft, startDate: _startDate, templateId: a.templateId),
      PlanBuilderMode.editPlan => api.update(plan: a.plan!, draft: _draft),
      PlanBuilderMode.newTemplate || PlanBuilderMode.editTemplate => api.saveTemplate(templateId: a.templateId, draft: _draft),
    };
  }

  static String _errorText(TrainerPlanStrings s, ApiException e) => switch (e.statusCode) {
        409 => s.conflict,
        403 => s.notYourClient,
        _ => s.saveFailed,
      };

  String _buttonLabel(TrainerPlanStrings s) => switch (_mode) {
        PlanBuilderMode.assign => '${s.assign} · ${s.workouts(_draft.workoutCount)}',
        PlanBuilderMode.editPlan => s.saveChanges,
        _ => s.saveTemplate,
      };

  Future<void> _pickStartDate() async {
    final today = DateUtils.dateOnly(DateTime.now());
    final picked = await showDatePicker(
      context: context,
      initialDate: _startDate,
      firstDate: today.subtract(const Duration(days: 7)),
      lastDate: today.add(const Duration(days: 60)),
    );
    if (picked != null) setState(() => _startDate = picked);
  }

  @override
  Widget build(BuildContext context) {
    final s = TrainerPlanStrings(_isArabic);
    final days = _days;
    return RtlScaffold(
      isArabic: _isArabic,
      appBar: AppBar(title: Text(_mode == PlanBuilderMode.assign ? s.givePlan : s.plan)),
      body: ListView(
        padding: const EdgeInsets.all(16),
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        children: [
          PlanBuilderHeader(draft: _draft, strings: s, week: _week, onWeek: (w) => setState(() => _week = w), onChanged: _changed),
          const SizedBox(height: 12),
          for (var i = 0; i < days.length; i++)
            PlanDayCard(
              key: ObjectKey(days[i]),
              day: days[i],
              index: i,
              strings: s,
              onChanged: _changed,
              onDuplicate: () => setState(() => days.insert(i + 1, days[i].copy())),
              onDelete: () => setState(() => days.removeAt(i)),
            ),
          if (days.length < PlanDraft.maxDaysPerWeek)
            OutlinedButton.icon(
              onPressed: () => setState(() => days.add(DayDraft())),
              icon: const Icon(Icons.add),
              label: Text(s.addDay),
            ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            if (_mode == PlanBuilderMode.assign)
              TextButton.icon(
                onPressed: _pickStartDate,
                icon: const Icon(Icons.event, size: 18),
                label: Text('${s.startDate}: ${MaterialLocalizations.of(context).formatMediumDate(_startDate)}'),
              ),
            FilledButton(
              onPressed: _saving ? null : () => _save(s),
              child: _saving
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(_buttonLabel(s)),
            ),
          ]),
        ),
      ),
    );
  }
}
