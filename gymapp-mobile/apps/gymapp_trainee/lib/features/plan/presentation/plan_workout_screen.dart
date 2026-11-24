import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';
import '../data/my_plan_view.dart';
import '../data/plan_providers.dart';
import '../data/workout_session.dart';
import 'trainee_plan_strings.dart';

/// One plan workout: every set prefilled (reps or time from the plan, weight from last time),
/// ticked off as the trainee goes. "Finish" saves on the phone first, then uploads.
class PlanWorkoutScreen extends ConsumerWidget {
  final String dayKey;

  const PlanWorkoutScreen({super.key, required this.dayKey});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final s = TraineePlanStrings(isArabic);
    return ref.watch(myPlanProvider).when(
          data: (view) {
            final slot = view?.slotFor(dayKey);
            if (view == null || slot == null) {
              return RtlScaffold(isArabic: isArabic, appBar: AppBar(), body: EmptyState(title: s.dayNotFound));
            }
            return _WorkoutBody(view: view, slot: slot, s: s);
          },
          loading: () => RtlScaffold(isArabic: isArabic, appBar: AppBar(), body: const Center(child: CircularProgressIndicator())),
          error: (_, __) => RtlScaffold(
            isArabic: isArabic,
            appBar: AppBar(),
            body: AppErrorWidget(message: s.loadError, onRetry: () => ref.invalidate(myPlanProvider), retryLabel: s.retry),
          ),
        );
  }
}

class _WorkoutBody extends ConsumerStatefulWidget {
  final MyPlanView view;
  final PlanSlot slot;
  final TraineePlanStrings s;

  const _WorkoutBody({required this.view, required this.slot, required this.s});

  @override
  ConsumerState<_WorkoutBody> createState() => _WorkoutBodyState();
}

class _WorkoutBodyState extends ConsumerState<_WorkoutBody> {
  late final WorkoutSession _session = WorkoutSession.start(widget.slot, widget.view.data.lastWeights);
  bool _saving = false;
  bool _finished = false;

  TraineePlanStrings get s => widget.s;

  @override
  Widget build(BuildContext context) {
    final day = widget.slot.day;
    return PopScope(
      canPop: _finished || !_session.hasProgress,
      onPopInvokedWithResult: (didPop, _) => didPop ? null : _confirmLeave(),
      child: RtlScaffold(
        isArabic: s.isArabic,
        appBar: AppBar(
          title: Text(day.displayTitle(s.isArabic)),
          bottom: PreferredSize(
            preferredSize: const Size.fromHeight(20),
            child: Text(s.weekLabel(widget.slot.week), style: Theme.of(context).textTheme.bodySmall),
          ),
        ),
        body: ListView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
          children: [
            for (final exercise in day.exercises)
              _ExerciseCard(
                exercise: exercise,
                session: _session,
                lastWeight: widget.view.data.lastWeights[exercise.key],
                mediaUrl: widget.view.data.exerciseMedia[exercise.key],
                s: s,
                onChanged: () => setState(() {}),
              ),
          ],
        ),
        bottomNavigationBar: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              key: const Key('plan-finish-workout'),
              text: _session.hasProgress ? s.finish(_session) : s.tickASet,
              isGradient: true,
              isLoading: _saving,
              onPressed: _session.hasProgress && !_saving ? _finish : null,
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _finish() async {
    setState(() => _saving = true);
    final userId = ref.read(authProvider).valueOrNull?.user?.id ?? '';
    final pending = _session.finish(userId: userId, plan: widget.view.plan);
    final result = await ref.read(planWorkoutRecorderProvider).finish(pending);
    if (!mounted) return;
    setState(() {
      _saving = false;
      _finished = true;
    });
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(_message(result))));
    context.pop();
  }

  String _message(FinishResult result) {
    if (!result.uploaded) return s.savedOffline;
    return switch (result.note) {
      PlanWorkoutOutcome.planChanged => s.notePlanChanged,
      PlanWorkoutOutcome.planReplaced => s.notePlanReplaced,
      PlanWorkoutOutcome.duplicate => s.noteDuplicate,
      _ => s.savedOnline,
    };
  }

  Future<void> _confirmLeave() async {
    final leave = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(s.leaveTitle),
        content: Text(s.leaveBody),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: Text(s.stay)),
          TextButton(onPressed: () => Navigator.pop(dialogContext, true), child: Text(s.leave)),
        ],
      ),
    );
    if (leave == true && mounted) {
      setState(() => _finished = true);
      context.pop();
    }
  }
}

class _ExerciseCard extends StatelessWidget {
  final PlanExerciseModel exercise;
  final WorkoutSession session;
  final double? lastWeight;
  final String? mediaUrl;
  final TraineePlanStrings s;
  final VoidCallback onChanged;

  const _ExerciseCard({
    required this.exercise,
    required this.session,
    required this.lastWeight,
    required this.mediaUrl,
    required this.s,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final sets = session.setsOf(exercise.key);
    return AppCard(
      margin: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            key: ValueKey('exercise-demo-${exercise.key}'),
            onTap: () => _openDemo(context),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              ExerciseMediaView(url: mediaUrl),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(exercise.displayName(s.isArabic), style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  Text(_subtitle(), style: theme.textTheme.bodySmall),
                  if (exercise.notes?.isNotEmpty ?? false)
                    Padding(padding: const EdgeInsets.only(top: 4), child: Text(exercise.notes!, style: theme.textTheme.bodySmall)),
                ]),
              ),
            ]),
          ),
          const SizedBox(height: 8),
          for (var i = 0; i < sets.length; i++)
            _SetRow(
              key: ValueKey('${exercise.key}-$i'),
              set: sets[i],
              timed: exercise.isTimed,
              s: s,
              onToggle: () {
                session.toggle(SetRef(exercise.key, i));
                onChanged();
              },
              // No rebuild while typing (it would reset the field); the next sets pick up
              // the new weight on the next tick.
              onWeight: (kg) => session.setWeight(SetRef(exercise.key, i), kg),
              onReps: (reps) => session.setReps(SetRef(exercise.key, i), reps),
            ),
        ],
      ),
    );
  }

  /// The big looping demo, the coach's notes and their video link.
  void _openDemo(BuildContext context) => showExerciseMediaSheet(
        context,
        ExerciseMediaDetails(
          title: exercise.displayName(s.isArabic),
          subtitle: _subtitle(),
          mediaUrl: mediaUrl,
          notes: exercise.notes,
          videoUrl: exercise.videoUrl,
        ),
        isArabic: s.isArabic,
      );

    String _subtitle() => [
        s.prescription(exercise),
        if (exercise.restSeconds != null) s.rest(exercise.restSeconds!),
        if (lastWeight != null) s.lastTime(lastWeight!),
      ].join(' · ');
}

class _SetRow extends StatelessWidget {
  final CompletedSetModel set;
  final bool timed;
  final TraineePlanStrings s;
  final VoidCallback onToggle;
  final ValueChanged<double?> onWeight;
  final ValueChanged<int?> onReps;

  const _SetRow({
    super.key,
    required this.set,
    required this.timed,
    required this.s,
    required this.onToggle,
    required this.onWeight,
    required this.onReps,
  });

  @override
  Widget build(BuildContext context) {
    final done = set.done;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 72, child: Text(s.setLabel(set))),
          Expanded(
            child: _NumberField(
              key: ValueKey('kg-${set.weightKg}'),
              value: set.weightKg == null ? '' : TraineePlanStrings.kgText(set.weightKg!),
              suffix: s.kg,
              decimal: true,
              onChanged: (text) => onWeight(double.tryParse(text.replaceAll(',', '.'))),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: timed
                ? Center(child: Text('${set.seconds ?? 0} ${s.seconds}'))
                : _NumberField(
                    value: set.reps?.toString() ?? '',
                    suffix: s.reps,
                    onChanged: (text) => onReps(int.tryParse(text)),
                  ),
          ),
          IconButton(
            key: Key('set-tick-${set.exerciseKey}-${set.setNumber}'),
            tooltip: s.done,
            onPressed: onToggle,
            icon: Icon(done ? Icons.check_circle_rounded : Icons.radio_button_unchecked,
                color: done ? Colors.green.shade600 : Theme.of(context).colorScheme.outline, size: 30),
          ),
        ],
      ),
    );
  }
}

class _NumberField extends StatelessWidget {
  final String value;
  final String suffix;
  final bool decimal;
  final ValueChanged<String> onChanged;

  const _NumberField({super.key, required this.value, required this.suffix, required this.onChanged, this.decimal = false});

  @override
  Widget build(BuildContext context) => TextFormField(
        initialValue: value,
        textAlign: TextAlign.center,
        keyboardType: TextInputType.numberWithOptions(decimal: decimal),
        inputFormatters: [FilteringTextInputFormatter.allow(RegExp(decimal ? r'[0-9.,]' : r'[0-9]'))],
        decoration: InputDecoration(isDense: true, suffixText: suffix, border: const OutlineInputBorder()),
        onChanged: onChanged,
      );
}
