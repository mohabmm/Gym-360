import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../sessions/presentation/pt/exercises/exercise_inputs.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'trainer_plan_strings.dart';

/// Adds or edits one exercise: pick it from the library (Arabic or English search) or type a
/// free-text name, then sets, reps or time, rest and the optional extras.
Future<ExerciseDraft?> showPlanExerciseSheet(BuildContext context, {ExerciseDraft? initial}) =>
    showModalBottomSheet<ExerciseDraft>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
        child: FractionallySizedBox(heightFactor: 0.9, child: _ExerciseSheet(initial: initial)),
      ),
    );

class _ExerciseSheet extends StatefulWidget {
  final ExerciseDraft? initial;

  const _ExerciseSheet({this.initial});

  @override
  State<_ExerciseSheet> createState() => _ExerciseSheetState();
}

class _ExerciseSheetState extends State<_ExerciseSheet> {
  static const _restChoices = [30, 45, 60, 90, 120, 180];

  late final ExerciseDraft _draft = widget.initial == null ? ExerciseDraft() : (ExerciseDraft.from(widget.initial!.toModel())..key = widget.initial!.key);
  late bool _picking = _draft.name.isEmpty;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  void _pick(String name, {LibraryExercise? library}) => setState(() {
        _draft
          ..name = library?.name ?? name.trim()
          ..nameAr = library?.nameAr
          ..exerciseId = library?.id;
        if (_draft.videoUrl.isEmpty && library?.demoMediaUrl != null) _draft.videoUrl = library!.demoMediaUrl!;
        _picking = false;
      });

  @override
  Widget build(BuildContext context) {
    final s = TrainerPlanStrings(_isArabic);
    return Directionality(
      textDirection: _isArabic ? TextDirection.rtl : TextDirection.ltr,
      child: _picking ? _ExercisePicker(strings: s, onPicked: _pick) : _details(s),
    );
  }

  Widget _details(TrainerPlanStrings s) {
    final measure = _draft.isTimed ? ExerciseMeasure.time : ExerciseMeasure.reps;
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
      children: [
        ListTile(
          contentPadding: EdgeInsets.zero,
          title: Text(_draft.toModel().displayName(_isArabic), style: Theme.of(context).textTheme.titleMedium),
          subtitle: _draft.exerciseId == null ? null : Text(s.fromLibrary),
          trailing: IconButton(icon: const Icon(Icons.swap_horiz), onPressed: () => setState(() => _picking = true)),
        ),
        Text(s.setsLabel, style: Theme.of(context).textTheme.labelLarge),
        SetsStepper(value: _draft.sets, isArabic: _isArabic, onChanged: (v) => setState(() => _draft.sets = v ?? 1)),
        const SizedBox(height: 12),
        MeasureToggle(
          value: measure,
          isArabic: _isArabic,
          onChanged: (m) => setState(() => _draft.seconds = m == ExerciseMeasure.time ? (_draft.seconds ?? 30) : null),
        ),
        const SizedBox(height: 12),
        if (_draft.isTimed)
          ExerciseTextField(
            label: s.timeLabel,
            initial: '${_draft.seconds}',
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            onChanged: (v) => _draft.seconds = int.tryParse(v) ?? _draft.seconds,
          )
        else
          ExerciseTextField(
            label: s.repsLabel,
            initial: _draft.reps,
            keyboardType: TextInputType.text,
            inputFormatters: const [RepsInputFormatter()],
            onChanged: (v) => setState(() => _draft.reps = v),
          ),
        const SizedBox(height: 12),
        Text(s.restLabel, style: Theme.of(context).textTheme.labelLarge),
        Wrap(spacing: 8, children: [
          for (final r in _restChoices)
            ChoiceChip(
              label: Text(s.rest(r)),
              selected: _draft.restSeconds == r,
              onSelected: (_) => setState(() => _draft.restSeconds = r),
            ),
        ]),
        const SizedBox(height: 12),
        WeightField(weight: _draft.targetWeightKg, isArabic: _isArabic, label: s.targetWeight, onChanged: (v) => _draft.targetWeightKg = v),
        const SizedBox(height: 12),
        _RpeChips(value: _draft.rpe, label: s.rpeLabel, onChanged: (v) => setState(() => _draft.rpe = v)),
        const SizedBox(height: 12),
        ExerciseTextField(label: s.notesLabel, initial: _draft.notes, onChanged: (v) => _draft.notes = v),
        const SizedBox(height: 12),
        ExerciseTextField(label: s.videoLabel, initial: _draft.videoUrl, keyboardType: TextInputType.url, onChanged: (v) => _draft.videoUrl = v),
        const SizedBox(height: 20),
        FilledButton(onPressed: _draft.isValid ? () => Navigator.pop(context, _draft) : null, child: Text(s.done)),
      ],
    );
  }
}

class _RpeChips extends StatelessWidget {
  final double? value;
  final String label;
  final ValueChanged<double?> onChanged;

  const _RpeChips({required this.value, required this.label, required this.onChanged});

  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: Theme.of(context).textTheme.labelLarge),
        Wrap(spacing: 6, children: [
          for (var r = 6; r <= 10; r++)
            ChoiceChip(
              label: Text('$r'),
              selected: value == r,
              onSelected: (on) => onChanged(on ? r.toDouble() : null),
            ),
        ]),
      ]);
}

/// Search the library; any text can also be used as a free-text exercise.
class _ExercisePicker extends ConsumerStatefulWidget {
  final TrainerPlanStrings strings;
  final void Function(String name, {LibraryExercise? library}) onPicked;

  const _ExercisePicker({required this.strings, required this.onPicked});

  @override
  ConsumerState<_ExercisePicker> createState() => _ExercisePickerState();
}

class _ExercisePickerState extends ConsumerState<_ExercisePicker> {
  String _query = '';

  @override
  Widget build(BuildContext context) {
    final s = widget.strings;
    final results = ref.watch(planExerciseSearchProvider(_query));
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
      children: [
        TextField(
          autofocus: true,
          decoration: InputDecoration(prefixIcon: const Icon(Icons.search), labelText: s.searchLibrary),
          onChanged: (v) => setState(() => _query = v),
        ),
        if (_query.trim().isNotEmpty)
          ListTile(
            leading: const Icon(Icons.edit_outlined),
            title: Text(s.useFreeText(_query.trim())),
            onTap: () => widget.onPicked(_query),
          ),
        ...results.maybeWhen(
          data: (list) => [
            for (final e in list)
              ListTile(
                leading: const Icon(Icons.fitness_center),
                title: Text(e.displayName(s.isArabic)),
                subtitle: s.isArabic && e.nameAr != null ? Text(e.name) : null,
                onTap: () => widget.onPicked(e.name, library: e),
              ),
          ],
          loading: () => const [LinearProgressIndicator()],
          orElse: () => const <Widget>[],
        ),
      ],
    );
  }
}
