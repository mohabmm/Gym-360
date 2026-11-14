import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'day_label_picker.dart';
import 'exercise_progress_providers.dart';
import 'exercise_row_editor.dart';
import 'session_exercises_draft.dart';

/// pt.exercise_progress: the coach records what the client did this session — day label,
/// sets × reps @ weight, a note and the next target per exercise. Picking a day the client
/// trained before pre-fills last time's exercises (or the targets the coach set).
class SessionExercisesScreen extends ConsumerStatefulWidget {
  final String sessionId;
  final String clientId;

  const SessionExercisesScreen({super.key, required this.sessionId, required this.clientId});

  @override
  ConsumerState<SessionExercisesScreen> createState() => _SessionExercisesScreenState();
}

class _SessionExercisesScreenState extends ConsumerState<SessionExercisesScreen> {
  final _draft = SessionExercisesDraft();
  bool _saving = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  String _t(String ar, String en) => _isArabic ? ar : en;

  void _pickDay(String? label) {
    final board = ref.read(clientExerciseProgressProvider(widget.clientId)).valueOrNull;
    setState(() {
      if (label == null) {
        _draft.dayLabel = null;
      } else if (_draft.applyRepeat(board, label)) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(_t('تمت تعبئة تمارين آخر ${ExerciseDays.display(label, true)}',
              "Filled in last $label's exercises")),
        ));
      }
    });
  }

  Future<void> _save() async {
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _saving = true);
    try {
      await saveSessionExercises(ref, sessionId: widget.sessionId, clientId: widget.clientId, draft: _draft);
      messenger.showSnackBar(SnackBar(content: Text(_t('تم حفظ التمارين', 'Exercises saved'))));
      router.pop();
    } on ApiException catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(_t('تعذر الحفظ، حاول مرة أخرى', "Couldn't save. Try again."))));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    // Warm the board so picking a day can pre-fill last time's numbers.
    ref.watch(clientExerciseProgressProvider(widget.clientId));
    return RtlScaffold(
      isArabic: _isArabic,
      appBar: AppBar(title: Text(_t('تمارين الحصة', 'Session exercises'))),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(_t('أي يوم تمرين؟', 'Which training day?'), style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          DayLabelPicker(selected: _draft.dayLabel, isArabic: _isArabic, onChanged: _pickDay),
          const SizedBox(height: 16),
          for (final row in _draft.rows)
            ExerciseRowEditor(
              key: ObjectKey(row),
              draft: row,
              isArabic: _isArabic,
              onChanged: () => setState(() {}),
              onRemove: () => setState(() => _draft.rows.remove(row)),
            ),
          OutlinedButton.icon(
            onPressed: () => setState(() => _draft.rows.add(ExerciseRowDraft())),
            icon: const Icon(Icons.add),
            label: Text(_t('أضف تمرين', 'Add exercise')),
          ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: FilledButton(
            onPressed: _draft.canSave && !_saving ? _save : null,
            child: _saving
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(_t('حفظ التمارين', 'Save exercises')),
          ),
        ),
      ),
    );
  }
}
